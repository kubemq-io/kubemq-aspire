using KubeMQ.Aspire.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQTlsWarningHostedServiceTests
{
    private static (KubeMQTlsWarningHostedService service, Mock<ILogger> logger) Create(
        bool useTls, bool hasAuthToken, string connectionName = "test")
    {
        var mockLogger = new Mock<ILogger>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        var service = new KubeMQTlsWarningHostedService(connectionName, useTls, hasAuthToken, mockLoggerFactory.Object);
        return (service, mockLogger);
    }

    [Fact]
    public async Task StartAsync_TlsEnabled_NoWarningsLogged()
    {
        var (service, logger) = Create(useTls: true, hasAuthToken: false);

        await service.StartAsync(CancellationToken.None);

        logger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_NoTls_NoToken_LogsWarningOnly()
    {
        var (service, logger) = Create(useTls: false, hasAuthToken: false);

        await service.StartAsync(CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        logger.Verify(
            l => l.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_NoTls_WithToken_LogsWarningAndCritical()
    {
        var (service, logger) = Create(useTls: false, hasAuthToken: true);

        await service.StartAsync(CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        logger.Verify(
            l => l.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var (service, _) = Create(useTls: false, hasAuthToken: false);

        var task = service.StopAsync(CancellationToken.None);

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }
}
