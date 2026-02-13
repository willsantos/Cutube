using System.Diagnostics.CodeAnalysis;

namespace Cutube.Cli;

[ExcludeFromCodeCoverage]
public class FileService : IFileService
{
    public bool Exists(string path) => File.Exists(path);

    public void Delete(string path) => File.Delete(path);

    public Task WriteAllBytesAsync(string path, byte[] data) =>
        File.WriteAllBytesAsync(path, data);

    public DateTime GetLastWriteTime(string path) =>
        File.GetLastWriteTime(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool HasWritePermission(string path)
    {
        try
        {
            var testFile = Path.Combine(path, Path.GetRandomFileName());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
