using Cutube.Api.Endpoints;
using Cutube.Api.HealthChecks;
using Cutube.Api.Hubs;
using Cutube.Api.Queuing;
using Cutube.Api.Services;
using Cutube.Contracts.Configuration;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Options;

// Program.cs is excluded from code coverage via GlobalSuppressions.cs or project configuration
var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "CORS origins are not configured. Set Cors:AllowedOrigins in appsettings.");
}

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Cutube API",
        Version = "v1",
        Description = "REST API for video downloads"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    // Keep-alive interval: servidor envia ping a cada 10s
    // para detectar conexões mortas
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);

    // Client timeout: se cliente não responder em 30s, desconecta
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

    // Handshake timeout: tempo máximo para handshake inicial
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);

    // Maximum message size (para progress events grandes)
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Domain Services
builder.Services.AddSingleton<IValidationService, FluentValidationService>();
builder.Services.AddSingleton<IMetadataService, FluentMetadataService>();

// Infrastructure (moved from CLI to Infrastructure project)
builder.Services.AddSingleton<IVideoDownloader, Cutube.Infrastructure.YtDlpDownloader>();
builder.Services.AddSingleton<IVideoProcessor, Cutube.Infrastructure.FfmpegProcessor>();
builder.Services.AddSingleton<IVideoMetadataProvider, Cutube.Infrastructure.YtDlpMetadataProvider>();

// API Services
builder.Services.AddSingleton<IDownloadStatusRepository, InMemoryStatusRepository>();
builder.Services.AddScoped<DlqService>();
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddHostedService<DownloadStatusCleanupService>();

// RabbitMQ Configuration - Skip if running in tests
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)
    );

    // MassTransit (Producer apenas - não configura consumers na API)
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            cfg.Host($"rabbitmq://{options.UserName}:{options.Password}@{options.Host}:{options.Port}{options.VirtualHost}");

            // Configurar retry
            cfg.UseMessageRetry(r =>
            {
                r.Interval(options.RetryCount, TimeSpan.FromSeconds(1));
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // Registrar producer
    builder.Services.AddScoped<IQueueProducer, RabbitMqProducer>();
}

// Configure options
builder.Services.Configure<DiskSpaceHealthCheckOptions>(
    builder.Configuration.GetSection(DiskSpaceHealthCheckOptions.SectionName));

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<YtDlpHealthCheck>("yt-dlp", tags: new[] { "ready" })
    .AddCheck<FfmpegHealthCheck>("ffmpeg", tags: new[] { "ready" })
    .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cutube API v1");
    });
}

app.UseCors("AllowNextJs");
app.UseHttpsRedirection();

// SignalR Hub endpoint
app.MapHub<DownloadHub>("/hubs/downloads");

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Map endpoints
app.MapDownloadsEndpoints();
app.MapVideosEndpoints();
app.MapStatusEndpoints();
app.MapDlqEndpoints();

// Root endpoint
app.MapGet("/", () => "Cutube API - Video Download Service");

app.Run();
