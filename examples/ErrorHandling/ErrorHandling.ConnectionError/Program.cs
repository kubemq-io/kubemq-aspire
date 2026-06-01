// KubeMQ Aspire — ErrorHandling: Connection Error
//
// Demonstrates handling initial connection failure with retry logic.

using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Exceptions;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

const int maxRetries = 3;
for (var attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        logger.LogInformation("Connection attempt {Attempt}/{Max}...", attempt, maxRetries);
        await client.ConnectAsync();
        logger.LogInformation("Connected successfully on attempt {Attempt}", attempt);
        break;
    }
    catch (KubeMQConnectionException ex)
    {
        logger.LogWarning(ex, "Connection failed on attempt {Attempt}", attempt);
        if (attempt == maxRetries)
        {
            logger.LogError("All {Max} connection attempts exhausted", maxRetries);
            return;
        }
        await Task.Delay(2000 * attempt); // exponential backoff
    }
}

await host.RunAsync();
