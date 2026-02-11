using Cutube.Api.Models;
using Cutube.Api.Services;
using Cutube.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cutube.Api.Tests.Unit.Services;

public class InMemoryStatusRepositoryTests
{
    private readonly InMemoryStatusRepository _repository;

    public InMemoryStatusRepositoryTests()
    {
        _repository = new InMemoryStatusRepository(NullLogger<InMemoryStatusRepository>.Instance);
    }

    [Fact]
    public async Task AddAsync_ShouldAddStatus()
    {
        // Arrange
        var status = new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(status, CancellationToken.None);
        var retrieved = await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-id");
        retrieved.Url.Should().Be("https://test.com");
        retrieved.Status.Should().Be(DownloadStatus.Queued);
    }

    [Fact]
    public async Task AddAsync_ShouldOverwriteIfIdAlreadyExists()
    {
        // Arrange
        var status1 = new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test1.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        var status2 = new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test2.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(status1, CancellationToken.None);
        await _repository.AddAsync(status2, CancellationToken.None);
        var retrieved =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Url.Should().Be("https://test2.com");
        retrieved.Status.Should().Be(DownloadStatus.Downloading);
        retrieved.Progress.Should().Be(50);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnStatusIfExists()
    {
        // Arrange
        var status = new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(status, CancellationToken.None);

        // Act
        var retrieved =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-id");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullIfNotExists()
    {
        // Act
        var retrieved = await _repository.GetByIdAsync("non-existent-id", CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDownloads()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "id1",
            CorrelationId = "id1",
            Url = "https://test1.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "id2",
            CorrelationId = "id2",
            Url = "https://test2.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        // Act
        var all = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        all.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyListIfRepositoryEmpty()
    {
        // Act
        var all = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        all.Should().NotBeNull();
        all.Count().Should().Be(0);
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldUpdateExistingDownload()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.UpdateProgressAsync("test-id", new Domain.Models.DownloadProgress
        {
            State = "downloading",
            Percentage = 50,
            DownloadedBytes = 1024,
            TotalBytes = 2048,
            Speed = 1024,
            ErrorMessage = null
        });

        var result =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        result!.Status.Should().Be(DownloadStatus.Downloading);
        result.Progress.Should().Be(50);
        result.DownloadedBytes.Should().Be(1024);
        result.TotalBytes.Should().Be(2048);
        result.Speed.Should().Be(1024);
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldMapStatesCorrectly()
    {
        // Test all state mappings
        await TestStateMapping("downloading", DownloadStatus.Downloading);
        await TestStateMapping("processing", DownloadStatus.Processing);
        await TestStateMapping("finished", DownloadStatus.Completed);
        await TestStateMapping("complete", DownloadStatus.Completed);
        await TestStateMapping("error", DownloadStatus.Failed);
        await TestStateMapping("failed", DownloadStatus.Failed);
    }

    private async Task TestStateMapping(string state, DownloadStatus expectedStatus)
    {
        // Arrange
        var testId = $"test-{state}";
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = testId,
            CorrelationId = testId,
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        // Act
        await _repository.UpdateProgressAsync(testId, new Domain.Models.DownloadProgress
        {
            State = state,
            Percentage = 50,
            DownloadedBytes = 0,
            TotalBytes = 0,
            Speed = 0,
            ErrorMessage = null
        });

        var result = await _repository.GetByCorrelationIdAsync(testId, CancellationToken.None);

        // Assert
        result!.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task UpdateProgressAsync_Finished_ShouldSetCompletedAt()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.UpdateProgressAsync("test-id", new Domain.Models.DownloadProgress
        {
            State = "finished",
            Percentage = 100,
            DownloadedBytes = 0,
            TotalBytes = 0,
            Speed = 0,
            ErrorMessage = null
        });

        var result =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        result!.Status.Should().Be(DownloadStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        result.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldNotFailIfIdNotExists()
    {
        // Act - Should not throw
        await _repository.UpdateProgressAsync("non-existent-id", new Domain.Models.DownloadProgress
        {
            State = "downloading",
            Percentage = 50,
            DownloadedBytes = 0,
            TotalBytes = 0,
            Speed = 0,
            ErrorMessage = null
        });

        // Assert - No exception thrown
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldUpdateErrorMessage()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.UpdateProgressAsync("test-id", new Domain.Models.DownloadProgress
        {
            State = "error",
            Percentage = 50,
            DownloadedBytes = 0,
            TotalBytes = 0,
            Speed = 0,
            ErrorMessage = "Download failed"
        });

        var result =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        result!.Status.Should().Be(DownloadStatus.Failed);
        result.ErrorMessage.Should().Be("Download failed");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDownload()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.DeleteAsync("test-id", CancellationToken.None);
        var result =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldBeIdempotent()
    {
        // Arrange
        await _repository.AddAsync(new DownloadStatusRecord
        {
            Id = "test-id",
            CorrelationId = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Act - Delete multiple times
        await _repository.DeleteAsync("test-id", CancellationToken.None);
        await _repository.DeleteAsync("test-id", CancellationToken.None);
        await _repository.DeleteAsync("test-id", CancellationToken.None);

        // Assert - No exception should be thrown
        var result =         await _repository.GetByCorrelationIdAsync("test-id", CancellationToken.None);
        result.Should().BeNull();
    }
}
