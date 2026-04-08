using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Config;
using KubeMQ.Sdk.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for registering KubeMQ clients in an Aspire-enabled application.
/// </summary>
public static class KubeMQClientExtensions
{
    private sealed class NonKeyedKubeMQRegistrationMarker { }

    private const string ActivitySourceName = "KubeMQ.Sdk";
    private const string MeterName = "KubeMQ.Sdk";

    /// <summary>
    /// Registers a KubeMQ client configured via Aspire connection string injection.
    /// Adds health checks, OpenTelemetry tracing and metrics by default.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">
    /// The connection name matching the resource name in the AppHost
    /// (e.g., "messaging" from <c>builder.AddKubeMQ("messaging")</c>).
    /// </param>
    /// <param name="configureSettings">Optional settings configuration delegate.</param>
    /// <param name="configureOptions">Optional SDK options configuration delegate.</param>
    public static void AddKubeMQClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<KubeMQClientSettings>? configureSettings = null,
        Action<KubeMQClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        if (builder.Services.Any(sd => sd.ServiceType == typeof(NonKeyedKubeMQRegistrationMarker)))
        {
            throw new InvalidOperationException(
                "AddKubeMQClient has already been called. For multiple KubeMQ connections, " +
                "use AddKeyedKubeMQClient with distinct service keys.");
        }

        builder.Services.AddSingleton<NonKeyedKubeMQRegistrationMarker>();

        var (settings, host, port) = BindAndResolveSettings(
            builder, connectionName, "Aspire:KubeMQ:Client", configureSettings);

        builder.Services.AddKubeMQ(opts =>
        {
            ApplySettings(opts, settings, host, port);
            configureOptions?.Invoke(opts);
        });

        RegisterTlsWarningAndObservability(builder, settings, connectionName);
    }

    /// <summary>
    /// Registers a keyed KubeMQ client for multi-instance scenarios.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="AddKubeMQClient"/>, the keyed path constructs the client directly
    /// rather than using SDK DI. This is because the SDK does not support keyed DI registration.
    /// Parity is verified by unit tests.
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="name">The keyed service name, also used to resolve the connection string.</param>
    /// <param name="configureSettings">Optional settings configuration delegate.</param>
    /// <param name="configureOptions">Optional SDK options configuration delegate.</param>
    public static void AddKeyedKubeMQClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<KubeMQClientSettings>? configureSettings = null,
        Action<KubeMQClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var (settings, host, port) = BindAndResolveSettings(
            builder, name, $"Aspire:KubeMQ:Client:{name}", configureSettings);

        var keyOptions = new KubeMQClientOptions();
        ApplySettings(keyOptions, settings, host, port);
        configureOptions?.Invoke(keyOptions);

        // NOTE: Keyed path constructs KubeMQClient directly because SDK's AddKubeMQ does not
        // support keyed DI. Any future changes to SDK's AddKubeMQ (e.g., additional hosted
        // services or wrapper registrations) will NOT automatically apply here.
        var client = new KubeMQClient(keyOptions);

        builder.Services.AddKeyedSingleton<IKubeMQClient>(name, (_, _) => client);
        builder.Services.AddSingleton<IHostedService>(
            _ => new KubeMQKeyedHostedService(client));

        RegisterTlsWarningAndObservability(builder, settings, name, isKeyed: true);
    }

    private static (KubeMQClientSettings Settings, string Host, int Port) BindAndResolveSettings(
        IHostApplicationBuilder builder,
        string connectionName,
        string configSectionPath,
        Action<KubeMQClientSettings>? configureSettings)
    {
        var settings = new KubeMQClientSettings();
        builder.Configuration.GetSection(configSectionPath).Bind(settings);
        configureSettings?.Invoke(settings);

        if (!settings.DisableHealthChecks && settings.HealthCheckTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings.HealthCheckTimeout),
                settings.HealthCheckTimeout,
                "Health check timeout must be a positive value.");
        }

        var connectionString = string.IsNullOrWhiteSpace(settings.ConnectionString)
            ? builder.Configuration.GetConnectionString(connectionName)
            : settings.ConnectionString;

        var (host, port) = ConnectionStringParser.Parse(connectionString, connectionName);
        return (settings, host, port);
    }

    private static void RegisterTlsWarningAndObservability(
        IHostApplicationBuilder builder,
        KubeMQClientSettings settings,
        string connectionName,
        bool isKeyed = false)
    {
        builder.Services.AddSingleton<IHostedService>(sp =>
            new KubeMQTlsWarningHostedService(connectionName, settings.UseTls,
                !string.IsNullOrEmpty(settings.AuthToken),
                sp.GetRequiredService<ILoggerFactory>()));

        ConfigureObservability(builder, settings, connectionName, isKeyed);
    }

    private static void ApplySettings(KubeMQClientOptions options, KubeMQClientSettings settings, string host, int port)
    {
        options.Address = $"{host}:{port}";

        if (settings.AuthToken is not null)
        {
            options.AuthToken = settings.AuthToken;
        }

        if (settings.ClientId is not null)
        {
            options.ClientId = settings.ClientId;
        }

        if (settings.DefaultTimeout.HasValue)
        {
            options.DefaultTimeout = settings.DefaultTimeout.Value;
        }

        if (settings.ConnectionTimeout.HasValue)
        {
            options.ConnectionTimeout = settings.ConnectionTimeout.Value;
        }

        if (settings.UseTls)
        {
            options.Tls = new TlsOptions
            {
                Enabled = true,
                CertFile = settings.TlsCertFile,
                KeyFile = settings.TlsKeyFile,
                CaFile = settings.TlsCaFile,
                ServerNameOverride = settings.TlsServerNameOverride,
                InsecureSkipVerify = settings.TlsInsecureSkipVerify,
            };
        }

        // gRPC tuning
        if (settings.GrpcChannelCount.HasValue)
        {
            options.GrpcChannelCount = settings.GrpcChannelCount.Value;
        }

        if (settings.MaxSendSize.HasValue)
        {
            options.MaxSendSize = settings.MaxSendSize.Value;
        }

        if (settings.MaxReceiveSize.HasValue)
        {
            options.MaxReceiveSize = settings.MaxReceiveSize.Value;
        }

        if (settings.WaitForReady.HasValue)
        {
            options.WaitForReady = settings.WaitForReady.Value;
        }

        // Keepalive
        if (settings.KeepalivePingInterval.HasValue)
        {
            options.Keepalive.PingInterval = settings.KeepalivePingInterval.Value;
        }

        if (settings.KeepalivePingTimeout.HasValue)
        {
            options.Keepalive.PingTimeout = settings.KeepalivePingTimeout.Value;
        }

        // Reconnect
        if (settings.ReconnectEnabled.HasValue)
        {
            options.Reconnect.Enabled = settings.ReconnectEnabled.Value;
        }

        if (settings.ReconnectMaxAttempts.HasValue)
        {
            options.Reconnect.MaxAttempts = settings.ReconnectMaxAttempts.Value;
        }

        if (settings.ReconnectTimeout.HasValue)
        {
            options.ReconnectTimeout = settings.ReconnectTimeout.Value;
        }
    }

    private static void ConfigureObservability(
        IHostApplicationBuilder builder,
        KubeMQClientSettings settings,
        string name,
        bool isKeyed = false)
    {
        if (!settings.DisableHealthChecks)
        {
            var healthCheckName = $"kubemq-{name}";
            var timeout = settings.HealthCheckTimeout;
            var readyName = $"{healthCheckName}-ready";
            var liveName = $"{healthCheckName}-live";

            builder.Services.AddKeyedSingleton<IHealthCheck>(readyName, (sp, _) =>
            {
                var client = isKeyed
                    ? sp.GetRequiredKeyedService<IKubeMQClient>(name)
                    : sp.GetRequiredService<IKubeMQClient>();
                return new KubeMQReadinessHealthCheck(client, timeout);
            });

            builder.Services.AddKeyedSingleton<IHealthCheck>(liveName, (sp, _) =>
            {
                var client = isKeyed
                    ? sp.GetRequiredKeyedService<IKubeMQClient>(name)
                    : sp.GetRequiredService<IKubeMQClient>();
                return new KubeMQLivenessHealthCheck(client);
            });

            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    readyName,
                    sp => sp.GetRequiredKeyedService<IHealthCheck>(readyName),
                    failureStatus: null,
                    tags: ["ready"]))
                .Add(new HealthCheckRegistration(
                    liveName,
                    sp => sp.GetRequiredKeyedService<IHealthCheck>(liveName),
                    failureStatus: null,
                    tags: ["live"]));
        }

        if (!settings.DisableTracing || !settings.DisableMetrics)
        {
            var otel = builder.Services.AddOpenTelemetry();
            if (!settings.DisableTracing)
            {
                otel.WithTracing(t => t.AddSource(ActivitySourceName));
            }

            if (!settings.DisableMetrics)
            {
                otel.WithMetrics(m => m.AddMeter(MeterName));
            }
        }
    }
}
