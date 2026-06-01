// KubeMQ Aspire — Config: Auth Token
//
// Configures authentication token for secure connections.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.AuthToken = "your-auth-token-here";
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await client.ConnectAsync();
    var info = await client.PingAsync();
    logger.LogInformation("Authenticated connection: Host={Host}", info.Host);
}
catch (Exception ex)
{
    logger.LogError(ex, "Authentication failed (expected if token is invalid)");
}

await host.RunAsync();
