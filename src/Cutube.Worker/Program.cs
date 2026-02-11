using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using Cutube.Worker.Handlers;
using Cutube.Worker.Services;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Cutube.Api.Queuing.Messages;

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

        // HttpClient for API communication
        services.AddHttpClient<IDownloadStatusNotificationService, DownloadStatusNotificationService>(client =>
        {
            var apiBaseUrl = context.Configuration.GetValue<string>("Worker:ApiBaseUrl") ?? "http://localhost:5000";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Domain services
        services.AddSingleton<IDownloadProcessingService, DownloadProcessingService>();

        // Retry Handler
        services.AddSingleton<RetryHandler>();

        // MassTransit (Consumer)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DownloadConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = context.GetRequiredService<RabbitMqOptions>();

                cfg.Host($"amqp://{rabbitMqConfig.UserName}:{rabbitMqConfig.Password}@{rabbitMqConfig.Host}:{rabbitMqConfig.Port}{rabbitMqConfig.VirtualHost}");

                cfg.ReceiveEndpoint(RabbitMqConfig.DownloadsQueue, e =>
                {
                    e.ConfigureConsumer<DownloadConsumer>(context);

                    // Prefetch count
                    e.PrefetchCount = RabbitMqConfig.PrefetchCount;

                    // Concurrent message limit
                    var workerOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkerOptions>>().Value;
                    e.ConcurrentMessageLimit = workerOptions.MaxConcurrentDownloads;

                    // Configurar DLQ usando argumentos da fila RabbitMQ
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

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message}");
            });
}
