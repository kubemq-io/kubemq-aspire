// KubeMQ Aspire — Config: TLS Setup
//
// Configures TLS encryption with a CA certificate.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.UseTls = true;
    settings.TlsCaFile = "/certs/ca.pem";
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await client.ConnectAsync();
    logger.LogInformation("Connected with TLS");
}
catch (Exception ex)
{
    logger.LogError(ex, "TLS connection failed (expected if certs not configured)");
}

await host.RunAsync();
