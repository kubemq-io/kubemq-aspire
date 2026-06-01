// KubeMQ Aspire — Config: Custom Timeouts
//
// Configures connection and operation timeouts.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.ConnectionTimeout = TimeSpan.FromSeconds(30);
    settings.DefaultTimeout = TimeSpan.FromSeconds(15);
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected with custom timeouts: ConnectionTimeout=30s, DefaultTimeout=15s");

var info = await client.PingAsync();
logger.LogInformation("Ping success: Host={Host}", info.Host);

await host.RunAsync();
