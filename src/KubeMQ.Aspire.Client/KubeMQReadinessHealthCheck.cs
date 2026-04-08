using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeMQ.Aspire.Client;

/// <summary>
/// Readiness health check for KubeMQ connectivity. Checks connection state first,
/// then calls PingAsync() only when the client is Ready.
/// </summary>
public sealed class KubeMQReadinessHealthCheck : IHealthCheck
{
    private readonly IKubeMQClient _client;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance of the KubeMQ readiness health check.</summary>
    /// <param name="client">The KubeMQ client to check.</param>
    /// <param name="timeout">The timeout for the ping operation.</param>
    public KubeMQReadinessHealthCheck(IKubeMQClient client, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout,
                "Health check timeout must be a positive value.");
        }

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

                    await _client.PingAsync(cts.Token).ConfigureAwait(false);

                    return HealthCheckResult.Healthy("KubeMQ connection is healthy");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    return HealthCheckResult.Unhealthy("KubeMQ health check timed out");
                }
                catch (Exception)
                {
                    return HealthCheckResult.Unhealthy(
                        "KubeMQ health check failed: unable to reach server");
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
