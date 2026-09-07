using Cutube.Cli.ErrorHandling;

namespace Cutube.Cli.Validation;

public static class InteractiveValidator
{
    private const int MaxAttempts = 3;

    public static string GetValidUrl(IConsoleService console, IErrorHandler errorHandler)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            console.Write("URL do vídeo: ");
            string? input = console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                ShowError(console, $"❌ URL não pode ser vazia. Use uma URL do YouTube (ex: https://youtube.com/watch?v=... ou https://youtu.be/...)", attempt);
                continue;
            }

            try
            {
                ValidationHelper.ValidateUrl(input);
                return input;
            }
            catch (Exception ex)
            {
                string message = errorHandler.GetUserFriendlyMessage(ex);
                ShowError(console, message, attempt);
            }
        }

        throw new InvalidOperationException("❌ Máximo de tentativas atingido para URL. Operação cancelada.");
    }

    public static string GetValidStartTime(IConsoleService console, IErrorHandler errorHandler)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            console.Write("Tempo de início (ex: 90s, 1:30, 1h30m): ");
            string? input = console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                ShowError(console, $"❌ Tempo de início não pode ser vazio. Use formatos como: 90s, 1:30, 1h30m.", attempt);
                continue;
            }

            try
            {
                var seconds = TimeHelper.ParseToSeconds(input);
                if (seconds <= 0)
                {
                    ShowError(console, $"❌ Tempo de início deve ser maior que zero. Use formatos como: 90s, 1:30, 1h30m.\nInício: {input}", attempt);
                    continue;
                }
                return input;
            }
            catch (Exception ex)
            {
                string message = errorHandler.GetUserFriendlyMessage(ex);
                ShowError(console, message, attempt);
            }
        }

        throw new InvalidOperationException("❌ Máximo de tentativas atingido para tempo de início. Operação cancelada.");
    }

    public static string GetValidEndTime(IConsoleService console, IErrorHandler errorHandler, string startTime)
    {
        var startSeconds = TimeHelper.ParseToSeconds(startTime);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            console.Write("Tempo de fim (ex: 120s, 2:00, 2m): ");
            string? input = console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                ShowError(console, $"❌ Tempo de fim não pode ser vazio. Use formatos como: 120s, 2:00, 2m.", attempt);
                continue;
            }

            try
            {
                var endSeconds = TimeHelper.ParseToSeconds(input);
                if (endSeconds <= startSeconds)
                {
                    ShowError(console, $"❌ Tempo de fim deve ser maior que o tempo de início ({startSeconds}s). Use formatos como: 120s, 2:00, 2m.\nFim: {input}", attempt);
                    continue;
                }
                return input;
            }
            catch (Exception ex)
            {
                string message = errorHandler.GetUserFriendlyMessage(ex);
                ShowError(console, message, attempt);
            }
        }

        throw new InvalidOperationException("❌ Máximo de tentativas atingido para tempo de fim. Operação cancelada.");
    }

    public static string GetValidFileName(IConsoleService console, IErrorHandler errorHandler)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            console.Write("Nome do arquivo (ou Enter para pular): ");
            string? input = console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            try
            {
                ValidationHelper.ValidateFileName(input);
                return input;
            }
            catch (Exception ex)
            {
                string message = errorHandler.GetUserFriendlyMessage(ex);
                ShowError(console, message, attempt);
            }
        }

        throw new InvalidOperationException("❌ Máximo de tentativas atingido para nome do arquivo. Operação cancelada.");
    }

    public static string GetValidDirectory(IConsoleService console, IErrorHandler errorHandler, IFileService fileService)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            console.Write("Diretório de saída (ou Enter para usar atual): ");
            string? input = console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            try
            {
                ValidationHelper.ValidateDirectory(input, fileService);
                return input.Trim();
            }
            catch (Exception ex)
            {
                string message = errorHandler.GetUserFriendlyMessage(ex);
                ShowError(console, message, attempt);
            }
        }

        throw new InvalidOperationException("❌ Máximo de tentativas atingido para diretório. Operação cancelada.");
    }

    private static void ShowError(IConsoleService console, string message, int attempt)
    {
        console.WriteLine(message);
        if (attempt < MaxAttempts)
        {
            console.WriteLine($"Tentativa {attempt} de {MaxAttempts}. Tente novamente.");
        }
    }
}
