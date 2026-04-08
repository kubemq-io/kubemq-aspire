using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQClientExtensionsTests
{
    private static HostApplicationBuilder CreateBuilderWithConnection(
        string connectionName = "messaging",
        string connectionString = "localhost:50000")
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = connectionString,
        });
        return builder;
    }

    [Fact]
    public void AddKubeMQClient_RegistersIKubeMQClient()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging");

        using var host = builder.Build();
        var client = host.Services.GetService<IKubeMQClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKubeMQClient_RegistersHealthCheck()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging");

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        Assert.Contains(options.Value.Registrations,
            r => r.Name == "kubemq-messaging-ready");
        Assert.Contains(options.Value.Registrations,
            r => r.Name == "kubemq-messaging-live");
    }

    [Fact]
    public void AddKubeMQClient_RegistersOTelTracing()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging");

        var hasTracingSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("TracerProvider", StringComparison.Ordinal));

        Assert.True(hasTracingSetup, "OpenTelemetry tracing should be registered");
    }

    [Fact]
    public void AddKubeMQClient_RegistersOTelMetrics()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging");

        var hasMetricsSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("MeterProvider", StringComparison.Ordinal));

        Assert.True(hasMetricsSetup, "OpenTelemetry metrics should be registered");
    }

    [Fact]
    public void AddKubeMQClient_DisableHealthChecks_SkipsRegistration()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.DisableHealthChecks = true;
        });

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.DoesNotContain(
            options.Value.Registrations,
            r => r.Name.StartsWith("kubemq-messaging", StringComparison.Ordinal));
    }

    [Fact]
    public void AddKubeMQClient_DisableTracing_SkipsRegistration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
        });

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.DisableTracing = true;
        });

        var hasTracingSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("TracerProvider", StringComparison.Ordinal));

        Assert.False(hasTracingSetup,
            "OpenTelemetry tracing should not be registered when disabled");
    }

    [Fact]
    public void AddKubeMQClient_DisableMetrics_SkipsRegistration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
        });

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.DisableMetrics = true;
        });

        var hasMetricsSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("MeterProvider", StringComparison.Ordinal));

        Assert.False(hasMetricsSetup,
            "OpenTelemetry metrics should not be registered when disabled");
    }

    [Fact]
    public void AddKubeMQClient_BindsConnectionString()
    {
        var builder = CreateBuilderWithConnection(
            connectionString: "myhost:55000");

        builder.AddKubeMQClient("messaging");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<IKubeMQClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKubeMQClient_MissingConnectionString_Throws()
    {
        var builder = Host.CreateApplicationBuilder();

        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("messaging"));

        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddKubeMQClient_MalformedConnectionString_Throws()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "http://localhost:50000",
        });

        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("messaging"));

        Assert.Contains("scheme prefix", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddKubeMQClient_SettingsPassthrough_AuthToken()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.AuthToken = "my-secret-token";
        });

        using var host = builder.Build();
        var client = host.Services.GetService<IKubeMQClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKubeMQClient_SettingsPassthrough_Timeouts()
    {
        var builder = CreateBuilderWithConnection();

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.DefaultTimeout = TimeSpan.FromSeconds(30);
            settings.ConnectionTimeout = TimeSpan.FromSeconds(10);
        });

        using var host = builder.Build();
        var client = host.Services.GetService<IKubeMQClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKubeMQClient_ConfigPriority_DelegateOverrides()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "config-host:50000",
            ["Aspire:KubeMQ:Client:ConnectionString"] = "section-host:50000",
        });

        builder.AddKubeMQClient("messaging", settings =>
        {
            settings.ConnectionString = "delegate-host:50000";
        });

        using var host = builder.Build();
        var client = host.Services.GetService<IKubeMQClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKeyedKubeMQClient_RegistersKeyedService()
    {
        var builder = CreateBuilderWithConnection(connectionName: "primary");

        builder.AddKeyedKubeMQClient("primary");

        using var host = builder.Build();
        var client = host.Services.GetKeyedService<IKubeMQClient>("primary");
        Assert.NotNull(client);
    }

    [Fact]
    public void AddKeyedKubeMQClient_SeparateHostedServices()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:first"] = "host1:50000",
            ["ConnectionStrings:second"] = "host2:50000",
        });

        var hostedServiceCountBefore = builder.Services
            .Count(sd => sd.ServiceType == typeof(IHostedService));

        builder.AddKeyedKubeMQClient("first");
        builder.AddKeyedKubeMQClient("second");

        var hostedServiceCountAfter = builder.Services
            .Count(sd => sd.ServiceType == typeof(IHostedService));

        Assert.True(hostedServiceCountAfter >= hostedServiceCountBefore + 2,
            $"Expected at least 2 new IHostedService registrations, got {hostedServiceCountAfter - hostedServiceCountBefore}");

        using var host = builder.Build();
        var first = host.Services.GetKeyedService<IKubeMQClient>("first");
        var second = host.Services.GetKeyedService<IKubeMQClient>("second");
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void UseTls_True_SetsTlsOptions()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings => { settings.UseTls = true; },
            opts => { capturedOptions = opts; });

        // Build and resolve the client to trigger the options configuration lambda
        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions!.Tls);
        Assert.True(capturedOptions.Tls.Enabled);
    }

    [Fact]
    public void UseTls_False_NoTlsOptions()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings => { settings.UseTls = false; },
            opts => { capturedOptions = opts; });

        // Build and resolve the client to trigger the options configuration lambda
        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Null(capturedOptions!.Tls);
    }

    [Fact]
    public void HealthCheckName_IncludesConnectionName()
    {
        var builder = CreateBuilderWithConnection();
        builder.AddKubeMQClient("messaging");

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        Assert.Contains(options.Value.Registrations,
            r => r.Name == "kubemq-messaging-ready");
        Assert.Contains(options.Value.Registrations,
            r => r.Name == "kubemq-messaging-live");
    }

    [Fact]
    public void KeyedClient_DisableFlags_Respected()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        builder.AddKeyedKubeMQClient("test", settings =>
        {
            settings.DisableHealthChecks = true;
            settings.DisableTracing = true;
            settings.DisableMetrics = true;
        });

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.DoesNotContain(options.Value.Registrations,
            r => r.Name.StartsWith("kubemq-test", StringComparison.Ordinal));
    }

    [Fact]
    public void KeyedConfig_BindsFromNamedSection()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mykey"] = "localhost:50000",
            ["Aspire:KubeMQ:Client:mykey:AuthToken"] = "keyed-token",
        });

        builder.AddKeyedKubeMQClient("mykey");

        using var host = builder.Build();
        var client = host.Services.GetKeyedService<IKubeMQClient>("mykey");
        Assert.NotNull(client);
    }

    [Fact]
    public void KeyedAndNonKeyed_SameSettings_SameConfig()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
            ["ConnectionStrings:keyed"] = "localhost:50000",
            ["Aspire:KubeMQ:Client:AuthToken"] = "shared-token",
            ["Aspire:KubeMQ:Client:keyed:AuthToken"] = "shared-token",
        });

        builder.AddKubeMQClient("messaging");
        builder.AddKeyedKubeMQClient("keyed");

        using var host = builder.Build();
        var nonKeyed = host.Services.GetRequiredService<IKubeMQClient>();
        var keyed = host.Services.GetRequiredKeyedService<IKubeMQClient>("keyed");
        Assert.NotNull(nonKeyed);
        Assert.NotNull(keyed);
        // Both clients registered successfully with equivalent settings
    }

    [Fact]
    public void ApplySettings_ConfiguresAllProperties()
    {
        var builder = CreateBuilderWithConnection(
            connectionString: "myhost:55000");

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.AuthToken = "test-token";
                settings.ClientId = "test-client";
                settings.DefaultTimeout = TimeSpan.FromSeconds(30);
                settings.ConnectionTimeout = TimeSpan.FromSeconds(15);
                settings.UseTls = true;
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Equal("myhost:55000", capturedOptions!.Address);
        Assert.Equal("test-token", capturedOptions.AuthToken);
        Assert.Equal("test-client", capturedOptions.ClientId);
        Assert.Equal(TimeSpan.FromSeconds(30), capturedOptions.DefaultTimeout);
        Assert.Equal(TimeSpan.FromSeconds(15), capturedOptions.ConnectionTimeout);
        Assert.NotNull(capturedOptions.Tls);
        Assert.True(capturedOptions.Tls!.Enabled);
    }

    [Fact]
    public void AddKubeMQClient_NullBuilder_Throws()
    {
        IHostApplicationBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(
            () => KubeMQClientExtensions.AddKubeMQClient(builder, "messaging"));
    }

    [Fact]
    public void AddKeyedKubeMQClient_NullBuilder_Throws()
    {
        IHostApplicationBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(
            () => KubeMQClientExtensions.AddKeyedKubeMQClient(builder, "messaging"));
    }

    // --- Phase 1: M-12 whitespace ConnectionString fallback ---

    [Fact]
    public void AddKubeMQClient_WhitespaceConnectionString_FallsBackToConfig()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
        });

        var exception = Record.Exception(() =>
            builder.AddKubeMQClient("messaging", settings =>
            {
                settings.ConnectionString = "   ";
            }));

        Assert.Null(exception);
    }

    [Fact]
    public void AddKeyedKubeMQClient_WhitespaceConnectionString_FallsBackToConfig()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        var exception = Record.Exception(() =>
            builder.AddKeyedKubeMQClient("test", settings =>
            {
                settings.ConnectionString = "   ";
            }));

        Assert.Null(exception);
    }

    // --- Phase 1: H-1 single non-keyed registration ---

    [Fact]
    public void AddKubeMQClient_CalledTwice_ThrowsInvalidOperation()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
            ["ConnectionStrings:other"] = "localhost:50001",
        });

        builder.AddKubeMQClient("messaging");

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.AddKubeMQClient("other"));
        Assert.Contains("AddKeyedKubeMQClient", ex.Message);
    }

    [Fact]
    public void AddKubeMQClient_ThenKeyed_Succeeds()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "localhost:50000",
            ["ConnectionStrings:secondary"] = "localhost:50001",
        });

        builder.AddKubeMQClient("messaging");
        var exception = Record.Exception(() =>
            builder.AddKeyedKubeMQClient("secondary"));

        Assert.Null(exception);
    }

    // --- Phase 1: M-10 HealthCheckTimeout validation ---

    [Fact]
    public void AddKubeMQClient_ZeroHealthCheckTimeout_Throws()
    {
        var builder = CreateBuilderWithConnection();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddKubeMQClient("messaging", settings =>
            {
                settings.HealthCheckTimeout = TimeSpan.Zero;
            }));
    }

    [Fact]
    public void AddKubeMQClient_NegativeHealthCheckTimeout_Throws()
    {
        var builder = CreateBuilderWithConnection();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddKubeMQClient("messaging", settings =>
            {
                settings.HealthCheckTimeout = TimeSpan.FromSeconds(-1);
            }));
    }

    [Fact]
    public void AddKubeMQClient_DisabledHealthChecks_InvalidTimeout_DoesNotThrow()
    {
        var builder = CreateBuilderWithConnection();

        var exception = Record.Exception(() =>
            builder.AddKubeMQClient("messaging", settings =>
            {
                settings.DisableHealthChecks = true;
                settings.HealthCheckTimeout = TimeSpan.Zero;
            }));

        Assert.Null(exception);
    }

    [Fact]
    public void AddKeyedKubeMQClient_ZeroHealthCheckTimeout_Throws()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.AddKeyedKubeMQClient("test", settings =>
            {
                settings.HealthCheckTimeout = TimeSpan.Zero;
            }));
    }

    // --- Phase 2: M-2 TLS sub-properties ---

    [Fact]
    public void UseTls_WithCertFiles_MapsTlsOptions()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.UseTls = true;
                settings.TlsCertFile = "/certs/client.pem";
                settings.TlsKeyFile = "/certs/client.key";
                settings.TlsCaFile = "/certs/ca.pem";
                settings.TlsServerNameOverride = "kubemq.example.com";
                settings.TlsInsecureSkipVerify = true;
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions!.Tls);
        Assert.True(capturedOptions.Tls!.Enabled);
        Assert.Equal("/certs/client.pem", capturedOptions.Tls.CertFile);
        Assert.Equal("/certs/client.key", capturedOptions.Tls.KeyFile);
        Assert.Equal("/certs/ca.pem", capturedOptions.Tls.CaFile);
        Assert.Equal("kubemq.example.com", capturedOptions.Tls.ServerNameOverride);
        Assert.True(capturedOptions.Tls.InsecureSkipVerify);
    }

    [Fact]
    public void UseTls_False_TlsSubPropertiesIgnored()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.UseTls = false;
                settings.TlsCertFile = "/certs/client.pem";
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Null(capturedOptions!.Tls);
    }

    // --- Phase 2: M-3 gRPC tuning options ---

    [Fact]
    public void ApplySettings_GrpcTuning_MapsAllProperties()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.GrpcChannelCount = 8;
                settings.MaxSendSize = 52_428_800;
                settings.MaxReceiveSize = 52_428_800;
                settings.WaitForReady = false;
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Equal(8, capturedOptions!.GrpcChannelCount);
        Assert.Equal(52_428_800, capturedOptions.MaxSendSize);
        Assert.Equal(52_428_800, capturedOptions.MaxReceiveSize);
        Assert.False(capturedOptions.WaitForReady);
    }

    [Fact]
    public void ApplySettings_KeepaliveOptions_MapsProperties()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.KeepalivePingInterval = TimeSpan.FromSeconds(20);
                settings.KeepalivePingTimeout = TimeSpan.FromSeconds(10);
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Equal(TimeSpan.FromSeconds(20), capturedOptions!.Keepalive.PingInterval);
        Assert.Equal(TimeSpan.FromSeconds(10), capturedOptions.Keepalive.PingTimeout);
    }

    [Fact]
    public void ApplySettings_ReconnectOptions_MapsProperties()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            settings =>
            {
                settings.ReconnectEnabled = false;
                settings.ReconnectMaxAttempts = 5;
                settings.ReconnectTimeout = TimeSpan.FromMinutes(2);
            },
            opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.False(capturedOptions!.Reconnect.Enabled);
        Assert.Equal(5, capturedOptions.Reconnect.MaxAttempts);
        Assert.Equal(TimeSpan.FromMinutes(2), capturedOptions.ReconnectTimeout);
    }

    [Fact]
    public void ApplySettings_NullOptionalProperties_UsesDefaults()
    {
        var builder = CreateBuilderWithConnection();

        KubeMQClientOptions? capturedOptions = null;
        builder.AddKubeMQClient("messaging",
            configureOptions: opts => { capturedOptions = opts; });

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(capturedOptions);
        Assert.Equal(5, capturedOptions!.GrpcChannelCount);
        Assert.True(capturedOptions.WaitForReady);
        Assert.True(capturedOptions.Reconnect.Enabled);
    }

    // --- Phase 3: H-4 connection-string precedence tests ---

    [Fact]
    public void ConfigPriority_SettingsConnectionString_WinsOverConnectionStrings()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "connstr-host:50000",
            ["Aspire:KubeMQ:Client:ConnectionString"] = "settings-host:50000",
        });

        KubeMQClientOptions? captured = null;
        builder.AddKubeMQClient("messaging",
            configureOptions: o => captured = o);

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(captured);
        Assert.Equal("settings-host:50000", captured!.Address);
    }

    [Fact]
    public void ConfigPriority_DelegateOverrides_BothSources()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "connstr-host:50000",
            ["Aspire:KubeMQ:Client:ConnectionString"] = "settings-host:50000",
        });

        KubeMQClientOptions? captured = null;
        builder.AddKubeMQClient("messaging",
            s => s.ConnectionString = "delegate-host:50000",
            o => captured = o);

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<IOptions<KubeMQClientOptions>>().Value;

        Assert.NotNull(captured);
        Assert.Equal("delegate-host:50000", captured!.Address);
    }

    [Fact]
    public void ConfigPriority_Keyed_SettingsConnectionString_WinsOverConnectionStrings()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mykey"] = "connstr-host:50000",
            ["Aspire:KubeMQ:Client:mykey:ConnectionString"] = "settings-host:50000",
        });

        KubeMQClientOptions? captured = null;
        builder.AddKeyedKubeMQClient("mykey",
            configureOptions: o => captured = o);

        Assert.NotNull(captured);
        Assert.Equal("settings-host:50000", captured!.Address);
    }

    [Fact]
    public void ConfigPriority_Keyed_DelegateOverrides_BothSources()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mykey"] = "connstr-host:50000",
            ["Aspire:KubeMQ:Client:mykey:ConnectionString"] = "settings-host:50000",
        });

        KubeMQClientOptions? captured = null;
        builder.AddKeyedKubeMQClient("mykey",
            s => s.ConnectionString = "delegate-host:50000",
            o => captured = o);

        Assert.NotNull(captured);
        Assert.Equal("delegate-host:50000", captured!.Address);
    }

    // --- Phase 3: M-16 test gaps ---

    [Fact]
    public void TlsWarning_UseTlsFalse_ServiceRegistered()
    {
        var builder = CreateBuilderWithConnection();
        builder.AddKubeMQClient("messaging", s => s.UseTls = false);

        using var host = builder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is KubeMQTlsWarningHostedService);
    }

    [Fact]
    public void TlsWarning_UseTlsTrue_ServiceStillRegistered()
    {
        var builder = CreateBuilderWithConnection();
        builder.AddKubeMQClient("messaging", s => s.UseTls = true);

        using var host = builder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is KubeMQTlsWarningHostedService);
    }

    [Fact]
    public void AddKeyedKubeMQClient_EmptyName_Throws()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        Assert.Throws<ArgumentException>(
            () => builder.AddKeyedKubeMQClient(""));
        Assert.Throws<ArgumentException>(
            () => builder.AddKeyedKubeMQClient("   "));
    }

    [Fact]
    public void KeyedClient_DisableTracing_SkipsRegistration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        builder.AddKeyedKubeMQClient("test", settings =>
        {
            settings.DisableTracing = true;
        });

        var hasTracingSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("TracerProvider", StringComparison.Ordinal));

        Assert.False(hasTracingSetup,
            "OpenTelemetry tracing should not be registered when disabled for keyed client");
    }

    [Fact]
    public void KeyedClient_DisableMetrics_SkipsRegistration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = "localhost:50000",
        });

        builder.AddKeyedKubeMQClient("test", settings =>
        {
            settings.DisableMetrics = true;
        });

        var hasMetricsSetup = builder.Services.Any(sd =>
            sd.ServiceType.FullName != null &&
            sd.ServiceType.FullName.Contains("MeterProvider", StringComparison.Ordinal));

        Assert.False(hasMetricsSetup,
            "OpenTelemetry metrics should not be registered when disabled for keyed client");
    }

    // --- Phase 2: M-4 singleton health check ---

    [Fact]
    public void HealthCheck_FactoryReturnsSameInstance()
    {
        var builder = CreateBuilderWithConnection();
        builder.AddKubeMQClient("messaging");

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        var readyReg = options.Value.Registrations.Single(r => r.Name == "kubemq-messaging-ready");

        var instance1 = readyReg.Factory(host.Services);
        var instance2 = readyReg.Factory(host.Services);

        Assert.Same(instance1, instance2);
    }
}
