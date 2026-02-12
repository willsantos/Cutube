using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cutube.Api.Queuing;
using Cutube.Contracts.Messages;
using Moq;

namespace Cutube.Api.Tests.Helpers;

/// <summary>
/// Factory for creating a test instance of API application
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing to skip MassTransit configuration
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Mock IQueueProducer for tests
            var mockProducer = new Mock<IQueueProducer>();
            mockProducer
                .Setup(x => x.PublishDownloadAsync(It.IsAny<DownloadMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DownloadMessage message, CancellationToken _) => message.CorrelationId);
            mockProducer.Setup(x => x.IsConnected).Returns(true);

            // Replace the real producer with the mock
            services.AddSingleton<IQueueProducer>(mockProducer.Object);
        });
    }
}
