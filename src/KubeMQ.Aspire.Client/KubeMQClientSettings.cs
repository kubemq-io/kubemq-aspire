namespace KubeMQ.Aspire.Client;

/// <summary>
/// Settings for configuring KubeMQ client integration with Aspire.
/// Bound from the "Aspire:KubeMQ:Client" configuration section.
/// </summary>
public sealed class KubeMQClientSettings
{
    /// <summary>Gets or sets the connection string (host:port).</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets whether to disable health check registration. Default: false.</summary>
    public bool DisableHealthChecks { get; set; }
    /// <summary>Gets or sets whether to disable OpenTelemetry tracing. Default: false.</summary>
    public bool DisableTracing { get; set; }
    /// <summary>Gets or sets whether to disable OpenTelemetry metrics. Default: false.</summary>
    public bool DisableMetrics { get; set; }
    /// <summary>Gets or sets the health check timeout. Default: 5 seconds.</summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Gets or sets the authentication token (passthrough to SDK).</summary>
    public string? AuthToken { get; set; }
    /// <summary>Gets or sets the client identifier (passthrough to SDK).</summary>
    public string? ClientId { get; set; }
    /// <summary>Gets or sets the SDK operation timeout (passthrough). Null uses SDK default (5s).</summary>
    public TimeSpan? DefaultTimeout { get; set; }
    /// <summary>Gets or sets the SDK connection timeout (passthrough). Null uses SDK default (10s).</summary>
    public TimeSpan? ConnectionTimeout { get; set; }
    /// <summary>Gets or sets whether to enable TLS for gRPC connections. Default: false (plain HTTP for dev containers).</summary>
    public bool UseTls { get; set; }

    // --- TLS sub-properties (M-2) ---

    /// <summary>Gets or sets the path to the client TLS certificate file (PEM). Optional.</summary>
    public string? TlsCertFile { get; set; }
    /// <summary>Gets or sets the path to the client TLS private key file (PEM). Optional.</summary>
    public string? TlsKeyFile { get; set; }
    /// <summary>Gets or sets the path to the CA certificate file (PEM). Optional.</summary>
    public string? TlsCaFile { get; set; }
    /// <summary>Gets or sets the server name override for TLS verification. Optional.</summary>
    public string? TlsServerNameOverride { get; set; }
    /// <summary>Gets or sets whether to skip TLS certificate verification (dev only). Default: false.</summary>
    public bool TlsInsecureSkipVerify { get; set; }

    // --- gRPC tuning options (M-3) ---

    /// <summary>Gets or sets the number of gRPC channels to pool. Null uses SDK default (5).</summary>
    public int? GrpcChannelCount { get; set; }
    /// <summary>Gets or sets the max send message size in bytes. Null uses SDK default (100 MB).</summary>
    public int? MaxSendSize { get; set; }
    /// <summary>Gets or sets the max receive message size in bytes. Null uses SDK default (100 MB).</summary>
    public int? MaxReceiveSize { get; set; }
    /// <summary>Gets or sets whether operations wait for connection to be ready. Null uses SDK default (true).</summary>
    public bool? WaitForReady { get; set; }

    // --- Keepalive options ---

    /// <summary>Gets or sets the keepalive ping interval. Null uses SDK default (10s).</summary>
    public TimeSpan? KeepalivePingInterval { get; set; }
    /// <summary>Gets or sets the keepalive ping timeout. Null uses SDK default (5s).</summary>
    public TimeSpan? KeepalivePingTimeout { get; set; }

    // --- Reconnect options ---

    /// <summary>Gets or sets whether auto-reconnect is enabled. Null uses SDK default (true).</summary>
    public bool? ReconnectEnabled { get; set; }
    /// <summary>Gets or sets the max reconnect attempts (0=unlimited). Null uses SDK default (0).</summary>
    public int? ReconnectMaxAttempts { get; set; }
    /// <summary>Gets or sets the reconnect timeout. Null uses SDK default (60s).</summary>
    public TimeSpan? ReconnectTimeout { get; set; }
}
