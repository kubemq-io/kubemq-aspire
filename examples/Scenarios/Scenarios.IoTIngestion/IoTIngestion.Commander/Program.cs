// KubeMQ Aspire — Scenario: IoT Ingestion — Commander
//
// Sends device commands and handles command responses.

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
logger.LogInformation("IoT Commander started");

var stoppingToken = lifetime.ApplicationStopping;

// Command handler (simulates device)
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new CommandsSubscription { Channel = "commands.iot.device" };
        await foreach (var cmd in client.SubscribeToCommandsAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Device] Received command: {Body}", Encoding.UTF8.GetString(cmd.Body.Span));
            await client.SendCommandResponseAsync(new CommandResponse
            {
                RequestId = cmd.RequestId,
                ReplyChannel = cmd.ReplyChannel!,
                Executed = true,
            }, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

// Send commands periodically
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);
        var commands = new[] { "calibrate", "reset", "report-status" };
        foreach (var cmd in commands)
        {
            var response = await client.SendCommandAsync(new CommandMessage
            {
                Channel = "commands.iot.device",
                Body = Encoding.UTF8.GetBytes(cmd),
                TimeoutInSeconds = 10
            }, stoppingToken);
            logger.LogInformation("[Commander] {Cmd}: Executed={Executed}", cmd, response.Executed);
            await Task.Delay(2000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
