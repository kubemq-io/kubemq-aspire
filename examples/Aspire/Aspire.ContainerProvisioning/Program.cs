// KubeMQ Aspire — Aspire: Container Provisioning
//
// Worker that demonstrates AppHost auto-provisioned KubeMQ container.
// The connection string is injected by Aspire.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
var info = await client.PingAsync();
logger.LogInformation("Connected to auto-provisioned KubeMQ container");
logger.LogInformation("  Host: {Host}", info.Host);
logger.LogInformation("  Version: {Version}", info.Version);
logger.LogInformation("  Uptime: {Uptime}s", info.ServerUpTimeSeconds);

await host.RunAsync();
