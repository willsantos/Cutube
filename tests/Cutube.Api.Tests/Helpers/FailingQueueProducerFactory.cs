using Cutube.Api.Queuing;
using Cutube.Api.Queuing.Exceptions;
using Cutube.Api.Queuing.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cutube.Api.Tests.Helpers;

public class FailingQueueProducerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var mockProducer = new Mock<IQueueProducer>();
            mockProducer
                .Setup(x => x.PublishDownloadAsync(It.IsAny<DownloadMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new QueuePublishException("Queue unavailable", "test-correlation-id"));
            mockProducer.Setup(x => x.IsConnected).Returns(false);

            services.AddSingleton<IQueueProducer>(mockProducer.Object);
        });
    }
}
