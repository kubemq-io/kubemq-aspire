// KubeMQ Aspire — Config: Environment Overrides
//
// Demonstrates overriding KubeMQ settings via appsettings.json and env vars.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Settings are bound from "Aspire:KubeMQ:Client" config section
// See appsettings.json for override values
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected with settings from appsettings.json / environment");

var info = await client.PingAsync();
logger.LogInformation("Host={Host}, Version={Version}", info.Host, info.Version);

await host.RunAsync();
