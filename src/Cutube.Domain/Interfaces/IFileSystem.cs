namespace Cutube.Domain.Interfaces;

/// <summary>
/// Abstracts file system operations for testability
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">File path to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    bool Exists(string path);

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="path">File path to delete</param>
    void Delete(string path);

    /// <summary>
    /// Writes bytes to a file asynchronously
    /// </summary>
    /// <param name="path">File path to write to</param>
    /// <param name="data">Byte array to write</param>
    Task WriteAllBytesAsync(string path, byte[] data);

    /// <summary>
    /// Gets the last write time of a file
    /// </summary>
    /// <param name="path">File path to check</param>
    /// <returns>Last write time as DateTime</returns>
    DateTime GetLastWriteTime(string path);

    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    /// <param name="path">Directory path to check</param>
    /// <returns>True if directory exists, false otherwise</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Checks if write permission is available for a directory
    /// </summary>
    /// <param name="path">Directory path to check</param>
    /// <returns>True if write permission is available, false otherwise</returns>
    bool HasWritePermission(string path);

    /// <summary>
    /// Creates a directory
    /// </summary>
    /// <param name="path">Directory path to create</param>
    void CreateDirectory(string path);
}
