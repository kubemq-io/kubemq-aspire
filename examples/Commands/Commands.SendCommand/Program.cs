// KubeMQ Aspire — Commands: Send Command
//
// Sends a command and waits for execution confirmation from a handler.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Commands;
using KubeMQ.Sdk.Exceptions;

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

// NOTE: Run Commands.HandleCommand in parallel for this to work
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000, stoppingToken);
        var response = await client.SendCommandAsync(new CommandMessage
        {
            Channel = "commands.send",
            Body = Encoding.UTF8.GetBytes("restart-service"),
            TimeoutInSeconds = 10
        }, stoppingToken);

        logger.LogInformation("Command result: Executed={Executed}, Error={Error}",
            response.Executed, response.Error ?? "none");
    }
    catch (KubeMQTimeoutException)
    {
        logger.LogWarning("Command timed out — no handler responded");
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
