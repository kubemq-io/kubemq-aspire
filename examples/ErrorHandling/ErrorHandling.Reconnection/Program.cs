// KubeMQ Aspire — ErrorHandling: Reconnection
//
// Demonstrates auto-reconnection behavior after broker restart.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.ReconnectEnabled = true;
    settings.ReconnectMaxAttempts = 0; // unlimited
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ with auto-reconnect enabled");

client.StateChanged += (_, args) =>
{
    logger.LogInformation("State: {Old} -> {New}", args.PreviousState, args.CurrentState);
};

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            i++;
            try
            {
                await client.SendEventAsync(new EventMessage
                {
                    Channel = "events.reconnect",
                    Body = Encoding.UTF8.GetBytes($"Event #{i}")
                }, stoppingToken);
                logger.LogInformation("Sent event #{Counter}", i);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("Send failed (will retry on reconnect): {Message}", ex.Message);
            }
            await Task.Delay(3000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

logger.LogInformation("Try restarting the KubeMQ broker to see auto-reconnection");
await host.RunAsync();
