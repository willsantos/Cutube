using System.Diagnostics.CodeAnalysis;
using Cutube.ErrorHandling;

namespace cutube;

public interface IMenuService
{
    Result Show(IConsoleService console, IFileService fileService, IErrorHandler errorHandler);
    string Url { get; }
    string Start { get; }
    string End { get; }
    string CustomFileName { get; }
    string OutputDirectory { get; }
    bool AudioOnly { get; }
}

[ExcludeFromCodeCoverage]
public class MenuService : IMenuService
{
    public Result Show(IConsoleService console, IFileService fileService, IErrorHandler errorHandler) 
        => Menu.Show(console, fileService, errorHandler);
    public string Url => Menu.Url;
    public string Start => Menu.Start;
    public string End => Menu.End;
    public string CustomFileName => Menu.CustomFileName;
    public string OutputDirectory => Menu.OutputDirectory;
    public bool AudioOnly => Menu.AudioOnly;
}
