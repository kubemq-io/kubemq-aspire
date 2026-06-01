// KubeMQ Aspire — Commands: Command Timeout
//
// Demonstrates timeout when no handler responds within the deadline.

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

await client.ConnectAsync();
logger.LogInformation("Connected to KubeMQ");

try
{
    // Send with a very short timeout — no handler is running
    var response = await client.SendCommandAsync(new CommandMessage
    {
        Channel = "commands.timeout",
        Body = Encoding.UTF8.GetBytes("will-timeout"),
        TimeoutInSeconds = 2 // short timeout
    });
    logger.LogInformation("Unexpected success: Executed={Executed}", response.Executed);
}
catch (KubeMQTimeoutException)
{
    logger.LogInformation("Expected: Command timed out after 2 seconds (no handler)");
}
catch (KubeMQOperationException ex)
{
    logger.LogInformation("Expected: Operation error — {Message}", ex.Message);
}

await host.RunAsync();
