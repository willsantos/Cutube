using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using Cutube.Worker.Handlers;
using Cutube.Worker.Services;
using Cutube.Contracts.Configuration;
using MassTransit;
using Serilog;
using Microsoft.Extensions.Options;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    })
    .ConfigureServices((context, services) =>
    {
        // Configuration
        services.Configure<WorkerOptions>(
            context.Configuration.GetSection(WorkerOptions.SectionName)
        );

        services.Configure<RetryPolicyOptions>(
            context.Configuration.GetSection(RetryPolicyOptions.SectionName)
        );

        services.Configure<RabbitMqOptions>(
            context.Configuration.GetSection(RabbitMqOptions.SectionName)
        );

        // HttpClient for API communication
        services.AddHttpClient<IDownloadStatusNotificationService, DownloadStatusNotificationService>(client =>
        {
            var apiBaseUrl = context.Configuration.GetValue<string>("Worker:ApiBaseUrl") ?? "http://localhost:5000";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(HttpRetryPolicyFactory.Create(Console.WriteLine));

        // Domain services
        services.AddSingleton<IDownloadProcessingService, DownloadProcessingService>();

        // Handlers
        services.AddSingleton<RetryHandler>();
        services.AddSingleton<DeadLetterHandler>();

        // MassTransit (Consumer)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DownloadConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host($"amqp://{rabbitMqConfig.UserName}:{rabbitMqConfig.Password}@{rabbitMqConfig.Host}:{rabbitMqConfig.Port}{rabbitMqConfig.VirtualHost}");

                // Configurar endpoint principal com DLQ
                cfg.ReceiveEndpoint(RabbitMqConfig.DownloadsQueue, e =>
                {
                    e.ConfigureConsumer<DownloadConsumer>(context);

                    // Prefetch count
                    e.PrefetchCount = RabbitMqConfig.PrefetchCount;

                    // Concurrent message limit
                    var workerOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkerOptions>>().Value;
                    e.ConcurrentMessageLimit = workerOptions.MaxConcurrentDownloads;

                    // Keep explicit DLQ arguments to maintain compatibility with existing queue declarations.
                    e.SetQueueArgument("x-dead-letter-exchange", RabbitMqConfig.DlqExchange);
                    e.SetQueueArgument("x-dead-letter-routing-key", RabbitMqConfig.DlqRoutingKey);
                });
            });
        });

        // Worker service
        services.AddHostedService<Cutube.Worker.Worker>();
    })
    .Build();

await host.RunAsync();
