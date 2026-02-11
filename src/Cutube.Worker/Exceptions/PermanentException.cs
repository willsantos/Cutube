namespace Cutube.Worker.Exceptions;

/// <summary>
/// Exceção que indica uma falha permanente que não deve ser retentada.
/// </summary>
public class PermanentException : Exception
{
    public PermanentException(string message) : base(message) { }
    public PermanentException(string message, Exception innerException)
        : base(message, innerException) { }
}
