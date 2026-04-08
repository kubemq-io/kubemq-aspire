using KubeMQ.Aspire.Client;
using KubeMQ.Sdk.Client;
using Moq;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQKeyedHostedServiceTests
{
    [Fact]
    public async Task KeyedHostedService_StartAsync_CallsConnect()
    {
        var mockClient = new Mock<IKubeMQClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new KubeMQKeyedHostedService(mockClient.Object);
        await service.StartAsync(CancellationToken.None);

        mockClient.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KeyedHostedService_StopAsync_DisposesClient()
    {
        var mockClient = new Mock<IKubeMQClient>();
        mockClient.Setup(c => c.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var service = new KubeMQKeyedHostedService(mockClient.Object);
        await service.StopAsync(CancellationToken.None);

        mockClient.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
