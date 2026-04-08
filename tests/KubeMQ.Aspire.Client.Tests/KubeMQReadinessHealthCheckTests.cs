using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQReadinessHealthCheckTests
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

    [Fact]
    public async Task HealthCheck_Ready_Healthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateServerInfo());

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("KubeMQ connection is healthy", result.Description);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task HealthCheck_Ready_PingTimeout_Unhealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Timed out"));

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("timed out", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task HealthCheck_Ready_PingException_Unhealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unable to reach server", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task HealthCheck_Reconnecting_Degraded()
    {
        var mock = CreateMock(ConnectionState.Reconnecting);
        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("reconnecting", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthCheck_Idle_Unhealthy()
    {
        var mock = CreateMock(ConnectionState.Idle);
        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not connected", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthCheck_Closed_Unhealthy()
    {
        var mock = CreateMock(ConnectionState.Closed);
        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("disposed", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthCheck_Connecting_Unhealthy()
    {
        var mock = CreateMock(ConnectionState.Connecting);
        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("kubemq", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("connecting", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadinessCheck_InvalidTimeout_Throws()
    {
        var mock = CreateMock(ConnectionState.Ready);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new KubeMQReadinessHealthCheck(mock.Object, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new KubeMQReadinessHealthCheck(mock.Object, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task ReadinessCheck_PropagatesOperationCanceled()
    {
        var mock = CreateMock(ConnectionState.Ready);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext
                {
                    Registration = new HealthCheckRegistration("test", healthCheck, null, null),
                },
                cts.Token));
    }

    [Fact]
    public async Task ReadinessCheck_ErrorMessage_DoesNotExposeDetails()
    {
        var mock = CreateMock(ConnectionState.Ready);
        mock.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret-connection-info"));

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("secret-connection-info", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void ReadinessCheck_NullClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new KubeMQReadinessHealthCheck(null!, DefaultTimeout));
    }

    [Fact]
    public async Task ReadinessCheck_UnknownConnectionState_ReturnsUnhealthy()
    {
        var mock = new Mock<IKubeMQClient>();
        mock.SetupGet(c => c.State).Returns((ConnectionState)99);

        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unknown state", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ConnectionState.Idle)]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Reconnecting)]
    [InlineData(ConnectionState.Closed)]
    public async Task ReadinessCheck_NonReadyStates_NeverCallsPing(ConnectionState state)
    {
        var mock = CreateMock(state);
        var healthCheck = new KubeMQReadinessHealthCheck(mock.Object, DefaultTimeout);

        await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        mock.Verify(c => c.PingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
