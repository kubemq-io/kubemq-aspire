// KubeMQ Aspire — Config: Reconnection Policy
//
// Configures auto-reconnect behavior after connection loss.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.ReconnectEnabled = true;
    settings.ReconnectMaxAttempts = 0; // unlimited
    settings.ReconnectTimeout = TimeSpan.FromSeconds(60);
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected with reconnection policy: Enabled=true, MaxAttempts=unlimited, Timeout=60s");

// Listen for state changes
client.StateChanged += (_, args) =>
{
    logger.LogInformation("Connection state changed: {State}", args.CurrentState);
};

await host.RunAsync();
