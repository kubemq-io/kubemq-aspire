// KubeMQ Aspire — Config: Basic Connection
//
// Minimal connection using Aspire's default configuration.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Minimal — connection string injected by Aspire from AppHost
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();

// Ping to verify connection
var info = await client.PingAsync();
logger.LogInformation("Connected to KubeMQ: Host={Host}, Version={Version}",
    info.Host, info.Version);

await host.RunAsync();
