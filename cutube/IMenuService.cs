using System.Diagnostics.CodeAnalysis;

namespace cutube;

public interface IMenuService
{
    void Show();
    string Url { get; }
    string Start { get; }
    string End { get; }
    string CustomFileName { get; }
}

[ExcludeFromCodeCoverage]
public class MenuService : IMenuService
{
    public void Show() => Menu.Show();
    public string Url => Menu.Url;
    public string Start => Menu.Start;
    public string End => Menu.End;
    public string CustomFileName => Menu.CustomFileName;
}
