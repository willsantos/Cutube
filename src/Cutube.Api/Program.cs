using Cutube.Api.Endpoints;
using Cutube.Api.HealthChecks;
using Cutube.Api.Services;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Services;
using Cutube.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://cutube.dev",
                "https://www.cutube.dev"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Domain Services
builder.Services.AddSingleton<IValidationService, FluentValidationService>();
builder.Services.AddSingleton<IDownloadService, FluentDownloadService>();
builder.Services.AddSingleton<IMetadataService, FluentMetadataService>();

// Infrastructure (from CLI project)
builder.Services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
builder.Services.AddSingleton<IVideoProcessor, FfmpegProcessor>();
builder.Services.AddSingleton<IVideoMetadataProvider, YtDlpMetadataProvider>();

// API Services
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<IDownloadStatusRepository, InMemoryStatusRepository>();
builder.Services.AddHostedService<BackgroundDownloadWorker>();

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

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Map endpoints
app.MapDownloadsEndpoints();
app.MapVideosEndpoints();

// Root endpoint
app.MapGet("/", () => "Cutube API - Video Download Service");

app.Run();
