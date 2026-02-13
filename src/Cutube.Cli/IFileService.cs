namespace Cutube.Cli;

public interface IFileService
{
    bool Exists(string path);
    void Delete(string path);
    Task WriteAllBytesAsync(string path, byte[] data);
    DateTime GetLastWriteTime(string path);

    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    bool HasWritePermission(string path);
}
