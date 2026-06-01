// KubeMQ Aspire — Config: gRPC Tuning
//
// Tunes gRPC channel pool size, message sizes, and wait-for-ready.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.GrpcChannelCount = 10;
    settings.MaxSendSize = 50 * 1024 * 1024; // 50 MB
    settings.MaxReceiveSize = 50 * 1024 * 1024;
    settings.WaitForReady = true;
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected with gRPC tuning: ChannelCount=10, MaxSize=50MB, WaitForReady=true");

await host.RunAsync();
