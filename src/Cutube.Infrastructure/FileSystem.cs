using Cutube.Domain.Interfaces;

namespace Cutube.Infrastructure;

/// <summary>
/// Default implementation of IFileSystem using System.IO
/// </summary>
public class FileSystem : IFileSystem
{
    /// <inheritdoc/>
    public bool Exists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Silently fail if file doesn't exist
        }
    }

    /// <inheritdoc/>
    public async Task WriteAllBytesAsync(string path, byte[] data)
    {
        try
        {
            await File.WriteAllBytesAsync(path, data);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write to {path}", ex);
        }
    }

    /// <inheritdoc/>
    public DateTime GetLastWriteTime(string path)
    {
        try
        {
            return File.GetLastWriteTime(path);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <inheritdoc/>
    public bool DirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool HasWritePermission(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var testFile = Path.Combine(path, Guid.NewGuid().ToString());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public void CreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch
        {
            // Silently fail if directory already exists
        }
    }
}
