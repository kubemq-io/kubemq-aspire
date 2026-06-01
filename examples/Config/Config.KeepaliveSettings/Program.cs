// KubeMQ Aspire — Config: Keepalive Settings
//
// Configures gRPC keepalive ping interval and timeout.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.KeepalivePingInterval = TimeSpan.FromSeconds(15);
    settings.KeepalivePingTimeout = TimeSpan.FromSeconds(5);
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected with keepalive: PingInterval=15s, PingTimeout=5s");

await host.RunAsync();
