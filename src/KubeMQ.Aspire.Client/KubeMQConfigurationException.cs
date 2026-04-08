namespace KubeMQ.Aspire.Client;

/// <summary>
/// Exception thrown when KubeMQ client configuration is invalid,
/// such as a malformed connection string.
/// </summary>
public sealed class KubeMQConfigurationException : Exception
{
    /// <summary>Initializes a new instance with the specified error message.</summary>
    /// <param name="message">The message that describes the configuration error.</param>
    public KubeMQConfigurationException(string message) : base(message) { }
    /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the configuration error.</param>
    /// <param name="innerException">The exception that caused this configuration error.</param>
    public KubeMQConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
