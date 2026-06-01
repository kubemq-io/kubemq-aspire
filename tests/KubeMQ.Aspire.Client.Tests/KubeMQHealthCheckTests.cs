using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQHealthCheckTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private static Mock<IKubeMQClient> CreateMock(ConnectionState state)
    {
        var mock = new Mock<IKubeMQClient>();
        mock.SetupGet(c => c.State).Returns(state);
        return mock;
    }

    private static ServerInfo CreateServerInfo() =>
        new()
        {
            Host = "test-host",
            Version = "3.5.0",
            ServerUpTimeSeconds = 1234,
        };

    private static HealthCheckContext CreateContext(IHealthCheck instance) =>
        new()
        {
            Registration = new HealthCheckRegistration("kubemq", instance, null, null),
        };

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new KubeMQHealthCheck(null!, DefaultTimeout));
    }

    [Fact]
    public async Task Ready_PingSucceeds_ReturnsHealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateServerInfo());

        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("test-host", result.Description);
        Assert.Contains("3.5.0", result.Description);
        Assert.Equal("test-host", result.Data["host"]);
        Assert.Equal("3.5.0", result.Data["version"]);
        Assert.Equal(1234L, result.Data["uptime_seconds"]);
    }

    [Fact]
    public async Task Ready_PingThrows_ReturnsUnhealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ping failed", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Ready_PingTimesOut_ReturnsUnhealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("timed out"));

        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ping failed", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reconnecting_ReturnsDegraded()
    {
        var mock = CreateMock(ConnectionState.Reconnecting);
        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("reconnecting", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Connecting_ReturnsUnhealthy()
    {
        var mock = CreateMock(ConnectionState.Connecting);
        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("connecting", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Idle_ReturnsUnhealthy()
    {
        var mock = CreateMock(ConnectionState.Idle);
        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not connected", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Closed_ReturnsUnhealthy()
    {
        var mock = CreateMock(ConnectionState.Closed);
        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("disposed", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownState_ReturnsUnhealthy()
    {
        var mock = new Mock<IKubeMQClient>();
        mock.SetupGet(c => c.State).Returns((ConnectionState)99);

        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unknown state", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ConnectionState.Idle)]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Reconnecting)]
    [InlineData(ConnectionState.Closed)]
    public async Task NonReadyStates_NeverCallsPing(ConnectionState state)
    {
        var mock = CreateMock(state);
        var hc = new KubeMQHealthCheck(mock.Object, DefaultTimeout);
        await hc.CheckHealthAsync(CreateContext(hc));

        mock.Verify(c => c.PingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
