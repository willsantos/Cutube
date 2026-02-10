// tests/Cutube.Api.Tests/SignalR/SignalRConnectionTrackerTests.cs
using Cutube.Api.Hubs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cutube.Api.Tests.SignalR;

public class SignalRConnectionTrackerTests
{
    private readonly ConnectionTracker _tracker;
    private readonly ILogger<ConnectionTracker> _logger;

    public SignalRConnectionTrackerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<ConnectionTracker>();
        _tracker = new ConnectionTracker(_logger);
    }

    [Fact]
    public void AddConnection_ShouldTrackConnection()
    {
        // Arrange
        var downloadId = "download-123";
        var connectionId = "connection-abc";

        // Act
        _tracker.AddConnection(downloadId, connectionId);

        // Assert
        _tracker.HasActiveConnections(downloadId).Should().BeTrue();
        _tracker.GetActiveConnectionCount(downloadId).Should().Be(1);
    }

    [Fact]
    public void AddConnection_MultipleConnections_ShouldTrackAll()
    {
        // Arrange
        var downloadId = "download-456";
        var conn1 = "connection-1";
        var conn2 = "connection-2";

        // Act
        _tracker.AddConnection(downloadId, conn1);
        _tracker.AddConnection(downloadId, conn2);

        // Assert
        _tracker.GetActiveConnectionCount(downloadId).Should().Be(2);
    }

    [Fact]
    public void RemoveConnection_ShouldRemoveTracking()
    {
        // Arrange
        var downloadId = "download-789";
        var connectionId = "connection-xyz";
        _tracker.AddConnection(downloadId, connectionId);

        // Act
        _tracker.RemoveConnection(downloadId, connectionId);

        // Assert
        _tracker.HasActiveConnections(downloadId).Should().BeFalse();
        _tracker.GetActiveConnectionCount(downloadId).Should().Be(0);
    }

    [Fact]
    public void RemoveConnection_MultipleConnections_ShouldRemoveOnlyOne()
    {
        // Arrange
        var downloadId = "download-multi";
        var conn1 = "connection-1";
        var conn2 = "connection-2";
        _tracker.AddConnection(downloadId, conn1);
        _tracker.AddConnection(downloadId, conn2);

        // Act
        _tracker.RemoveConnection(downloadId, conn1);

        // Assert
        _tracker.HasActiveConnections(downloadId).Should().BeTrue();
        _tracker.GetActiveConnectionCount(downloadId).Should().Be(1);
    }

    [Fact]
    public void RemoveConnectionAll_ShouldRemoveAllAssociations()
    {
        // Arrange
        var connectionId = "connection-all";
        var download1 = "download-1";
        var download2 = "download-2";
        _tracker.AddConnection(download1, connectionId);
        _tracker.AddConnection(download2, connectionId);

        // Act
        _tracker.RemoveConnectionAll(connectionId);

        // Assert
        _tracker.HasActiveConnections(download1).Should().BeFalse();
        _tracker.HasActiveConnections(download2).Should().BeFalse();
    }

    [Fact]
    public void GetStats_ShouldReturnCorrectCounts()
    {
        // Arrange
        _tracker.AddConnection("download-1", "conn-1");
        _tracker.AddConnection("download-2", "conn-2");
        _tracker.AddConnection("download-3", "conn-1"); // conn-1 tem 2 downloads

        // Act
        var (totalDownloads, totalConnections) = _tracker.GetStats();

        // Assert
        totalDownloads.Should().Be(3);
        totalConnections.Should().Be(2);
    }
}
