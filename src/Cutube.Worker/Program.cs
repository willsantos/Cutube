using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
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

        // MassTransit (Consumer)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DownloadConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = context.GetRequiredService<RabbitMqOptions>();

                cfg.Host($"amqp://{rabbitMqConfig.UserName}:{rabbitMqConfig.Password}@{rabbitMqConfig.Host}:{rabbitMqConfig.Port}{rabbitMqConfig.VirtualHost}");

                cfg.ReceiveEndpoint("cutube.downloads", e =>
                {
                    e.ConfigureConsumer<DownloadConsumer>(context);

                    // Prefetch count: 1 message at a time (avoids overload)
                    e.PrefetchCount = 1;

                    // Concurrent message limit
                    var workerOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkerOptions>>().Value;
                    e.ConcurrentMessageLimit = workerOptions.MaxConcurrentDownloads;

                    // Retry policy: 3 attempts with exponential backoff
                    e.UseMessageRetry(r =>
                    {
                        r.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
                        r.Handle<Exception>();
                    });
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
