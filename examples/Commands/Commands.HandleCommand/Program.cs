// KubeMQ Aspire — Commands: Handle Command
//
// Subscribes to incoming commands and responds with execution status.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Commands;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ — waiting for commands on commands.send");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var subscription = new CommandsSubscription { Channel = "commands.send" };
        await foreach (var cmd in client.SubscribeToCommandsAsync(subscription, stoppingToken))
        {
            var body = Encoding.UTF8.GetString(cmd.Body.Span);
            logger.LogInformation("Received command: {Body}", body);

            // Process and respond
            await client.SendCommandResponseAsync(new CommandResponse
            {
                RequestId = cmd.RequestId,
                ReplyChannel = cmd.ReplyChannel!,
                Executed = true,
            }, stoppingToken);

            logger.LogInformation("Responded: executed=true");
        }
    }
    catch (OperationCanceledException) { /* shutting down */ }
}, stoppingToken);

await host.RunAsync();
