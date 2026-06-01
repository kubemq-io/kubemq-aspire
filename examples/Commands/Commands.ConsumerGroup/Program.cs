// KubeMQ Aspire — Commands: Consumer Group
//
// Multiple command handlers in a group — only one handles each command.

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
logger.LogInformation("Connected to KubeMQ");

var stoppingToken = lifetime.ApplicationStopping;

// Handler A — group "handlers"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new CommandsSubscription
        {
            Channel = "commands.group",
            Group = "handlers"
        };
        await foreach (var cmd in client.SubscribeToCommandsAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Handler-A] Received: {Body}", Encoding.UTF8.GetString(cmd.Body.Span));
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

// Handler B — same group "handlers"
_ = Task.Run(async () =>
{
    try
    {
        var subscription = new CommandsSubscription
        {
            Channel = "commands.group",
            Group = "handlers"
        };
        await foreach (var cmd in client.SubscribeToCommandsAsync(subscription, stoppingToken))
        {
            logger.LogInformation("[Handler-B] Received: {Body}", Encoding.UTF8.GetString(cmd.Body.Span));
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

// Sender — sends 5 commands; only one handler gets each
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);
        for (var i = 1; i <= 5; i++)
        {
            var response = await client.SendCommandAsync(new CommandMessage
            {
                Channel = "commands.group",
                Body = Encoding.UTF8.GetBytes($"Command #{i}"),
                TimeoutInSeconds = 10
            }, stoppingToken);
            logger.LogInformation("Command #{I} result: Executed={Executed}", i, response.Executed);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
