namespace AzureAI.Core.Domain.Exceptions;

/// <summary>Thrown when the completion service fails to generate a response.</summary>
public sealed class CompletionException : Exception
{
    /// <inheritdoc cref="CompletionException"/>
    /// <param name="message">Describes the failure.</param>
    /// <param name="innerException">Optional underlying exception.</param>
    public CompletionException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
