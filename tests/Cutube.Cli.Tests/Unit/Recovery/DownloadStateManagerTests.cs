using Cutube.Logging;
using Cutube.Recovery;
using Cutube.Cli;
using Moq;
using Xunit;

namespace Cutube.Tests.Unit.Recovery;

public class DownloadStateManagerTests : IDisposable
{
    private readonly DownloadStateManager _manager;
    private readonly MockEnvironmentService _envService;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly string _testStatePath;

    public DownloadStateManagerTests()
    {
        _testStatePath = Path.Combine(Path.GetTempPath(), $"cutube-state-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testStatePath);

        _envService = new MockEnvironmentService(_testStatePath);
        _loggerMock = new Mock<ILoggerService>();

        _manager = new DownloadStateManager(_envService, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateState_SavesJsonFile()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };

        // Act
        var stateId = await _manager.CreateStateAsync(state);

        // Debug: listar todos os arquivos criados
        var allFiles = Directory.GetFiles(_testStatePath, "*.json", SearchOption.AllDirectories);
        Assert.True(allFiles.Length > 0, $"No files found in {_testStatePath}");

        // Assert - o arquivo é salvo em _testStatePath/Cutube/state/
        var filePath = allFiles[0]; // Usar o primeiro arquivo encontrado
        Assert.True(File.Exists(filePath), $"Expected file at {filePath}");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("https://youtube.com/watch?v=test", content);
    }

    [Fact]
    public async Task GetState_ReturnsNull_WhenNotFound()
    {
        // Act
        var state = await _manager.GetStateAsync("nonexistent");

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task GetState_ReturnsState_WhenExists()
    {
        // Arrange
        var original = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };
        var stateId = await _manager.CreateStateAsync(original);

        // Act
        var retrieved = await _manager.GetStateAsync(stateId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(stateId, retrieved.StateId);
        Assert.Equal("https://youtube.com/watch?v=test", retrieved.Url);
    }

    [Fact]
    public async Task UpdateState_ModifiesState_Atomically()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading,
            ProgressPercent = 0
        };
        var stateId = await _manager.CreateStateAsync(state);

        // Act
        await _manager.UpdateStateAsync(stateId, s =>
        {
            s.ProgressPercent = 50;
            s.Status = DownloadStatus.Processing;
        });

        // Assert
        var updated = await _manager.GetStateAsync(stateId);
        Assert.Equal(50, updated!.ProgressPercent);
        Assert.Equal(DownloadStatus.Processing, updated.Status);
    }

    [Fact]
    public async Task MarkCompleted_UpdatesStatusAndProgress()
    {
        // Arrange
        var state = new DownloadState
        {
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/video.mp4",
            Status = DownloadStatus.Downloading
        };
        var stateId = await _manager.CreateStateAsync(state);

        // Act
        await _manager.MarkCompletedAsync(stateId);

        // Assert
        var completed = await _manager.GetStateAsync(stateId);
        Assert.Equal(DownloadStatus.Completed, completed!.Status);
        Assert.Equal(100, completed.ProgressPercent);
    }

    [Fact]
    public async Task CleanupOldStates_RemovesFiles_OlderThanMaxAge()
    {
        // Arrange - criar um estado diretamente com data antiga
        // Ao invés de modificar o arquivo, vamos deletar o manager original e criar um novo com estado antigo
        var oldDate = DateTime.UtcNow.AddDays(-10);

        // Criar estado manualmente no arquivo JSON com data antiga
        var oldStateId = Guid.NewGuid().ToString();
        var oldStateJson = System.Text.Json.JsonSerializer.Serialize(new DownloadState
        {
            StateId = oldStateId,
            Url = "https://youtube.com/watch?v=old",
            OutputPath = "/tmp/old.mp4",
            Status = DownloadStatus.Failed,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        }, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        });

        // Encontrar o diretório de estados e escrever o arquivo diretamente
        var allFiles = Directory.GetFiles(_testStatePath, "*.json", SearchOption.AllDirectories);
        string stateDir;
        if (allFiles.Length > 0)
        {
            stateDir = Path.GetDirectoryName(allFiles[0])!;
        }
        else
        {
            // Criar diretório se não existir
            stateDir = Path.Combine(_testStatePath, "Cutube", "state");
            Directory.CreateDirectory(stateDir);
        }

        var oldStatePath = Path.Combine(stateDir, $"{oldStateId}.json");
        await File.WriteAllTextAsync(oldStatePath, oldStateJson);

        // Debug: verificar se o arquivo existe
        Assert.True(File.Exists(oldStatePath), $"State file not found at {oldStatePath}");

        // Act
        var removed = await _manager.CleanupOldStatesAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, removed);
        Assert.False(File.Exists(oldStatePath));
    }

    [Fact]
    public async Task GetActiveStates_ReturnsOnlyNonCompletedStates()
    {
        // Arrange
        var activeState = new DownloadState
        {
            Url = "https://youtube.com/watch?v=active",
            OutputPath = "/tmp/active.mp4",
            Status = DownloadStatus.Downloading
        };
        await _manager.CreateStateAsync(activeState);

        var completedState = new DownloadState
        {
            Url = "https://youtube.com/watch?v=completed",
            OutputPath = "/tmp/completed.mp4",
            Status = DownloadStatus.Completed
        };
        await _manager.CreateStateAsync(completedState);

        // Act
        var activeStates = await _manager.GetActiveStatesAsync();

        // Assert
        Assert.Single(activeStates);
        Assert.Equal("https://youtube.com/watch?v=active", activeStates[0].Url);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStatePath))
        {
            Directory.Delete(_testStatePath, recursive: true);
        }
    }

    private class MockEnvironmentService : IEnvironmentService
    {
        private readonly string _basePath;

        public MockEnvironmentService(string basePath)
        {
            _basePath = basePath;
        }

        public string OSArchitecture => "x64";

        public string GetFolderPath(Environment.SpecialFolder folder) => _basePath;

        public string? GetEnvironmentVariable(string name) => null;

        public bool IsWindows() => false;

        public bool IsLinux() => true;

        public bool IsMacOS() => false;
    }
}
