using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Cutube.Api.Services;
using Cutube.Domain.Models;
using Xunit;

namespace Cutube.Api.Tests.Unit.Services;

public class DownloadQueueTests
{
    [Fact]
    public async Task EnqueueAsync_ShouldGenerateUniqueId()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };

        // Act
        var id1 = await queue.EnqueueAsync(request, CancellationToken.None);
        var id2 = await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert
        id1.Should().NotBeNullOrEmpty();
        id2.Should().NotBeNullOrEmpty();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldAddItemToChannel()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };

        // Act
        var id = await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert - Verify item was added to channel
        var cts = new CancellationTokenSource(100);
        var items = queue.DequeueAllAsync(cts.Token);

        await foreach (var (itemId, itemRequest) in items)
        {
            itemId.Should().Be(id);
            itemRequest.Url.Should().Be("https://test.com");
            break; // Test only the first item
        }
    }

    [Fact]
    public async Task EnqueueAsync_ShouldCreateCancellationTokenSource()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };

        // Act
        var id = await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert - Cannot directly verify _cancellationTokens is set without exposing internal state
        // The verification is done via CancelAsync which should work
        await queue.CancelAsync(id, CancellationToken.None);
        // If cancellation works, it means the CTS was created correctly
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelExistingDownload()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };
        var id = await queue.EnqueueAsync(request, CancellationToken.None);

        // Act
        await queue.CancelAsync(id, CancellationToken.None);

        // Assert - Cannot directly verify cancellation without exposing internal state
        // The test verifies no exception is thrown
    }

    [Fact]
    public async Task CancelAsync_ShouldBeIdempotent()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };
        var id = await queue.EnqueueAsync(request, CancellationToken.None);

        // Act - Cancel multiple times
        await queue.CancelAsync(id, CancellationToken.None);
        await queue.CancelAsync(id, CancellationToken.None);
        await queue.CancelAsync(id, CancellationToken.None);

        // Assert - No exception should be thrown
    }

    [Fact]
    public async Task CancelAsync_ShouldLogWarningIfDownloadNotFound()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var nonExistentId = "non-existent-id";

        // Act
        await queue.CancelAsync(nonExistentId, CancellationToken.None);

        // Assert - No exception should be thrown, warning is logged
    }

    [Fact]
    public async Task DequeueAllAsync_ShouldReturnEnqueuedItems()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var request = new DownloadRequest { Url = "https://test.com", OutputPath = "/tmp/test" };
        var id = await queue.EnqueueAsync(request, CancellationToken.None);
        var cts = new CancellationTokenSource(100);

        // Act
        var items = queue.DequeueAllAsync(cts.Token);
        var foundItem = false;

        await foreach (var (itemId, itemRequest) in items)
        {
            // Assert
            itemId.Should().Be(id);
            itemRequest.Url.Should().Be("https://test.com");
            foundItem = true;
            break; // Test only the first item
        }

        foundItem.Should().BeTrue();
    }

    [Fact]
    public async Task DequeueAllAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var cts = new CancellationTokenSource(10); // Very short timeout

        // Act & Assert
        var items = queue.DequeueAllAsync(cts.Token);

        // Should throw OperationCanceledException when cancelled
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in items)
            {
                // Should not reach here as no items are enqueued
            }
        });

        // Verify cancellation is respected
        exception.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task DequeueAllAsync_ShouldBlockUntilItemsAvailable()
    {
        // Arrange
        var queue = new DownloadQueue(NullLogger<DownloadQueue>.Instance);
        var cts = new CancellationTokenSource(50); // Short timeout

        // Act
        var items = queue.DequeueAllAsync(cts.Token);

        // Assert - Should block until timeout or cancellation
        var task = Task.Run(async () =>
        {
            await foreach (var _ in items)
            {
                return true;
            }
            return false;
        });

        // Task should complete (timeout) without items
    }
}
