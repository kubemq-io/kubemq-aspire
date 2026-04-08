using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeMQ.Aspire.Client;

/// <summary>
/// Health check for KubeMQ connectivity. Checks connection state first,
/// then calls PingAsync() only when the client is Ready.
/// </summary>
public sealed class KubeMQHealthCheck : IHealthCheck
{
    private readonly IKubeMQClient _client;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance of the KubeMQ health check.</summary>
    /// <param name="client">The KubeMQ client to check.</param>
    /// <param name="timeout">The timeout for the ping operation.</param>
    public KubeMQHealthCheck(IKubeMQClient client, TimeSpan timeout)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _timeout = timeout;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var state = _client.State;

        switch (state)
        {
            case ConnectionState.Ready:
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    var serverInfo = await _client.PingAsync(cts.Token).ConfigureAwait(false);

                    return HealthCheckResult.Healthy(
                        $"KubeMQ server {serverInfo.Host} v{serverInfo.Version} (uptime: {serverInfo.ServerUpTimeSeconds}s)",
                        new Dictionary<string, object>
                        {
                            ["host"] = serverInfo.Host,
                            ["version"] = serverInfo.Version,
                            ["uptime_seconds"] = serverInfo.ServerUpTimeSeconds,
                        });
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy(
                        $"KubeMQ ping failed: {ex.GetType().Name}: {ex.Message}",
                        exception: ex);
                }

            case ConnectionState.Reconnecting:
                return HealthCheckResult.Degraded("KubeMQ client is reconnecting");

            case ConnectionState.Connecting:
                return HealthCheckResult.Unhealthy("KubeMQ client is connecting");

            case ConnectionState.Idle:
                return HealthCheckResult.Unhealthy("KubeMQ client is not connected");

            case ConnectionState.Closed:
                return HealthCheckResult.Unhealthy("KubeMQ client has been disposed");

            default:
                return HealthCheckResult.Unhealthy($"KubeMQ client in unknown state: {state}");
        }
    }
}
