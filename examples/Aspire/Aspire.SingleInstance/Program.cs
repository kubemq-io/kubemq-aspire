// KubeMQ Aspire — Aspire: Single Instance
//
// Basic non-keyed KubeMQ client registration with AddKubeMQClient().

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
var info = await client.PingAsync();
logger.LogInformation("Single instance connected: Host={Host}, Version={Version}",
    info.Host, info.Version);

await host.RunAsync();
