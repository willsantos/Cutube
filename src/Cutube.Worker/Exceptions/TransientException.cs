namespace Cutube.Worker.Exceptions;

/// <summary>
/// Exceção que indica uma falha transitória que pode ser resolvida com retry.
/// </summary>
public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
    public TransientException(string message, Exception innerException)
        : base(message, innerException) { }
}
