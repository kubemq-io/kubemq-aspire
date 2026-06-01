// KubeMQ Aspire — Config: Mutual TLS (mTLS)
//
// Configures mutual TLS with both client and CA certificates.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.UseTls = true;
    settings.TlsCertFile = "/certs/client.pem";
    settings.TlsKeyFile = "/certs/client-key.pem";
    settings.TlsCaFile = "/certs/ca.pem";
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await client.ConnectAsync();
    logger.LogInformation("Connected with mutual TLS");
}
catch (Exception ex)
{
    logger.LogError(ex, "mTLS connection failed (expected if certs not configured)");
}

await host.RunAsync();
