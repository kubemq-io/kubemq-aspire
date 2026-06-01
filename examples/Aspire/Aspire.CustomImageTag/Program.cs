// KubeMQ Aspire — Aspire: Custom Image Tag
//
// Worker that connects to a KubeMQ instance pinned to a specific version.
// The version is set in AppHost: .WithImageTag("2.5.2")

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
var info = await client.PingAsync();
logger.LogInformation("Connected to KubeMQ version: {Version}", info.Version);
logger.LogInformation("Image tag is set in AppHost: builder.AddKubeMQ(\"messaging\").WithImageTag(\"...\")");

await host.RunAsync();
