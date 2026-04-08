using KubeMQ.Sdk.Client;
using Microsoft.Extensions.Hosting;

namespace KubeMQ.Aspire.Client;

internal sealed class KubeMQKeyedHostedService : IHostedService
{
    private readonly IKubeMQClient _client;

    public KubeMQKeyedHostedService(IKubeMQClient client) => _client = client;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _client.ConnectAsync(cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await _client.DisposeAsync().ConfigureAwait(false);
}
