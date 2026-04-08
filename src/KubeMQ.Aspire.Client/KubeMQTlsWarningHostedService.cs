using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Aspire.Client;

/// <summary>
/// Hosted service that logs a warning at startup if TLS is not enabled
/// for a KubeMQ client connection.
/// </summary>
internal sealed class KubeMQTlsWarningHostedService : IHostedService
{
    private readonly string _connectionName;
    private readonly bool _useTls;
    private readonly bool _hasAuthToken;
    private readonly ILogger _logger;

    public KubeMQTlsWarningHostedService(string connectionName, bool useTls, bool hasAuthToken, ILoggerFactory loggerFactory)
    {
        _connectionName = connectionName;
        _useTls = useTls;
        _hasAuthToken = hasAuthToken;
        _logger = loggerFactory.CreateLogger("KubeMQ.Aspire.Client");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_useTls)
        {
            _logger.LogWarning(
                "KubeMQ client '{ConnectionName}' is configured without TLS. " +
                "Set UseTls=true for production environments.",
                _connectionName);

            if (_hasAuthToken)
            {
                _logger.LogCritical(
                    "KubeMQ client '{ConnectionName}' has an authentication token configured without TLS. " +
                    "Credentials will be transmitted in plaintext. Enable UseTls=true to secure the connection.",
                    _connectionName);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
