// KubeMQ Aspire — Patterns: Request-Reply
//
// Synchronous request-reply pattern over Commands channel.

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
var stoppingToken = lifetime.ApplicationStopping;

// Responder
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new CommandsSubscription { Channel = "commands.reqreply" };
        await foreach (var cmd in client.SubscribeToCommandsAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Responder] Received: {Body}", Encoding.UTF8.GetString(cmd.Body.Span));
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

// Requester
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000, stoppingToken);
        for (var i = 1; i <= 3; i++)
        {
            var response = await client.SendCommandAsync(new CommandMessage
            {
                Channel = "commands.reqreply",
                Body = Encoding.UTF8.GetBytes($"Request #{i}"),
                TimeoutInSeconds = 10
            }, stoppingToken);
            logger.LogInformation("[Requester] Reply #{I}: Executed={Executed}", i, response.Executed);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
