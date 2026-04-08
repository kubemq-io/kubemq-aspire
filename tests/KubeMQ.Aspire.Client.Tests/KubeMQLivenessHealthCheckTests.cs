using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQLivenessHealthCheckTests
{
    private static Mock<IKubeMQClient> CreateMock(ConnectionState state)
    {
        var mock = new Mock<IKubeMQClient>();
        mock.SetupGet(c => c.State).Returns(state);
        return mock;
    }

    [Fact]
    public void LivenessCheck_NullClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new KubeMQLivenessHealthCheck(null!));
    }

    [Fact]
    public async Task LivenessCheck_Ready_ReturnsHealthy()
    {
        var mock = CreateMock(ConnectionState.Ready);
        var healthCheck = new KubeMQLivenessHealthCheck(mock.Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("ready", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LivenessCheck_Reconnecting_ReturnsDegraded()
    {
        var mock = CreateMock(ConnectionState.Reconnecting);
        var healthCheck = new KubeMQLivenessHealthCheck(mock.Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("reconnecting", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ConnectionState.Idle)]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Closed)]
    public async Task LivenessCheck_NonReadyNonReconnecting_ReturnsUnhealthy(ConnectionState state)
    {
        var mock = CreateMock(state);
        var healthCheck = new KubeMQLivenessHealthCheck(mock.Object);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("test", healthCheck, null, null),
            });

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
