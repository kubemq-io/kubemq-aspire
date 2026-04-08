using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeMQ.Aspire.Client;

/// <summary>
/// Lightweight liveness health check for KubeMQ client. Checks connection state only
/// without making any network calls. A failed liveness check indicates the process
/// should be restarted.
/// </summary>
public sealed class KubeMQLivenessHealthCheck : IHealthCheck
{
    private readonly IKubeMQClient _client;

    /// <summary>Initializes a new instance of the KubeMQ liveness health check.</summary>
    /// <param name="client">The KubeMQ client to check.</param>
    public KubeMQLivenessHealthCheck(IKubeMQClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var state = _client.State;
        var result = state switch
        {
            ConnectionState.Ready => HealthCheckResult.Healthy("KubeMQ client is ready"),
            ConnectionState.Reconnecting => HealthCheckResult.Degraded("KubeMQ client is reconnecting"),
            _ => HealthCheckResult.Unhealthy($"KubeMQ client state: {state}"),
        };
        return Task.FromResult(result);
    }
}
