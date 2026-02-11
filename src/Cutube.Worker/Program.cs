using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName)
);

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DownloadConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;

        cfg.Host($"amqp://{options.UserName}:{options.Password}@{options.Host}:{options.Port}{options.VirtualHost}");

        // Configure retry for connection issues
        cfg.UseMessageRetry(r =>
        {
            r.Incremental(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        });

        // Configure prefetch count
        cfg.PrefetchCount = RabbitMqConfig.PrefetchCount;

        // Configure receive endpoint for downloads queue
        cfg.ReceiveEndpoint(RabbitMqConfig.DownloadsQueue, e =>
        {
            e.ConfigureConsumer<DownloadConsumer>(context);
            e.ConfigureDownloadsQueue();
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Health checks - basic TCP check for RabbitMQ
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

var host = builder.Build();
host.Run();
