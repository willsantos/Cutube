using System.Diagnostics.CodeAnalysis;
using Cutube.ErrorHandling;
using cutube.Validation;

namespace Cutube.Cli;


[ExcludeFromCodeCoverage]
public static class Menu
{
    public static string Url { get; private set; } = string.Empty;
    public static string Start { get; private set; } = string.Empty;
    public static string End { get; private set; } = string.Empty;
    public static string CustomFileName { get; private set; } = string.Empty;
    public static string OutputDirectory { get; private set; } = string.Empty;
    public static bool AudioOnly { get; private set; } = false;

    internal static void Reset()
    {
        Url = string.Empty;
        Start = string.Empty;
        End = string.Empty;
        CustomFileName = string.Empty;
        OutputDirectory = string.Empty;
        AudioOnly = false;
    }

    public static Result Show(IConsoleService console, IFileService fileService, IErrorHandler errorHandler)
    {
        try
        {
            console.WriteLine("\n=== Download de Cortes do YouTube ===\n");

            Url = InteractiveValidator.GetValidUrl(console, errorHandler);

            Start = InteractiveValidator.GetValidStartTime(console, errorHandler);

            End = InteractiveValidator.GetValidEndTime(console, errorHandler, Start);

            var fileNameInput = InteractiveValidator.GetValidFileName(console, errorHandler);
            CustomFileName = string.IsNullOrWhiteSpace(fileNameInput) ? string.Empty : Path.GetFileNameWithoutExtension(fileNameInput.Trim());

            OutputDirectory = InteractiveValidator.GetValidDirectory(console, errorHandler, fileService);

            AudioOnly = GetAudioOnlyChoice(console);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            console.WriteLine(ex.Message);
            return Result.Failure(Cutube.ErrorHandling.ErrorType.Validation, ex.Message, ex);
        }
    }

    private static bool GetAudioOnlyChoice(IConsoleService console)
    {
        console.Write("\nTipo de download:\n1 - Vídeo\n2 - Áudio apenas\nEscolha: ");
        string? choice = console.ReadLine();
        return choice?.Trim() == "2";
    }
}
