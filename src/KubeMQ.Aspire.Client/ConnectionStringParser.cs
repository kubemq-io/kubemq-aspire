namespace KubeMQ.Aspire.Client;

internal static class ConnectionStringParser
{
    internal static (string Host, int Port) Parse(string? connectionString, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new KubeMQConfigurationException(
                $"Connection string for '{connectionName}' is missing.");
        }

        connectionString = connectionString.Trim();

        if (connectionString.Contains("://", StringComparison.Ordinal))
        {
            throw new KubeMQConfigurationException(
                $"Connection string for '{connectionName}' must be plain host:port without scheme prefix.");
        }

        string host;
        string portStr;

        if (connectionString.StartsWith('['))
        {
            var bracketEnd = connectionString.IndexOf("]:", StringComparison.Ordinal);
            if (bracketEnd < 0)
            {
                throw new KubeMQConfigurationException(
                    $"Connection string for '{connectionName}' has invalid IPv6 format. Expected '[host]:port'.");
            }

            host = connectionString[1..bracketEnd];
            portStr = connectionString[(bracketEnd + 2)..];
        }
        else
        {
            var lastColon = connectionString.LastIndexOf(':');
            if (lastColon < 0)
            {
                throw new KubeMQConfigurationException(
                    $"Connection string for '{connectionName}' must be in host:port format.");
            }

            host = connectionString[..lastColon];
            portStr = connectionString[(lastColon + 1)..];

            if (host.Contains(':'))
            {
                throw new KubeMQConfigurationException(
                    $"Connection string for '{connectionName}' appears to contain an IPv6 address without brackets. " +
                    "Use '[address]:port' format (e.g., '[::1]:50000').");
            }
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new KubeMQConfigurationException(
                $"Connection string for '{connectionName}' has an empty host component.");
        }

        if (!int.TryParse(portStr, out var port) || port < 1 || port > 65535)
        {
            throw new KubeMQConfigurationException(
                $"Connection string port must be an integer between 1 and 65535. Got: '{portStr}'");
        }

        return (host, port);
    }
}
