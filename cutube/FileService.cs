using System.Diagnostics.CodeAnalysis;

namespace cutube;

[ExcludeFromCodeCoverage]
public class FileService : IFileService
{
    public bool Exists(string path) => File.Exists(path);

    public void Delete(string path) => File.Delete(path);

    public Task WriteAllBytesAsync(string path, byte[] data) => 
        File.WriteAllBytesAsync(path, data);

    public DateTime GetLastWriteTime(string path) => 
        File.GetLastWriteTime(path);
}
