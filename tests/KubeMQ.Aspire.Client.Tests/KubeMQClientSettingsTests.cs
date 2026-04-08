using System.Text.Json;
using KubeMQ.Aspire.Client;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQClientSettingsTests
{
    [Fact]
    public void ConfigSchema_ValidJson()
    {
        var assembly = typeof(KubeMQClientSettings).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "KubeMQ.Aspire.Client.ConfigurationSchema.json");

        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.False(string.IsNullOrWhiteSpace(json));

        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("$schema", out var schema));
        Assert.Equal("https://json-schema.org/draft-07/schema#", schema.GetString());

        Assert.True(root.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("Aspire", out _));
    }

    [Fact]
    public void ConfigBinding_AspireSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:KubeMQ:Client:ConnectionString"] = "myhost:50000",
                ["Aspire:KubeMQ:Client:DisableHealthChecks"] = "true",
                ["Aspire:KubeMQ:Client:DisableTracing"] = "false",
                ["Aspire:KubeMQ:Client:DisableMetrics"] = "true",
                ["Aspire:KubeMQ:Client:HealthCheckTimeout"] = "00:00:10",
                ["Aspire:KubeMQ:Client:AuthToken"] = "my-token",
                ["Aspire:KubeMQ:Client:ClientId"] = "test-client",
                ["Aspire:KubeMQ:Client:UseTls"] = "true",
            })
            .Build();

        var settings = new KubeMQClientSettings();
        config.GetSection("Aspire:KubeMQ:Client").Bind(settings);

        Assert.Equal("myhost:50000", settings.ConnectionString);
        Assert.True(settings.DisableHealthChecks);
        Assert.False(settings.DisableTracing);
        Assert.True(settings.DisableMetrics);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.HealthCheckTimeout);
        Assert.Equal("my-token", settings.AuthToken);
        Assert.Equal("test-client", settings.ClientId);
        Assert.True(settings.UseTls);
    }

    [Fact]
    public void UseTls_DefaultsFalse()
    {
        var settings = new KubeMQClientSettings();
        Assert.False(settings.UseTls);
    }
}
