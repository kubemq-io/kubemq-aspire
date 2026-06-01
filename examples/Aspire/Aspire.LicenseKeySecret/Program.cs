// KubeMQ Aspire — Aspire: License Key Secret
//
// Demonstrates passing the KubeMQ license key as an Aspire secret parameter.
// The license key is stored securely and passed as KUBEMQ_TOKEN env var.

using KubeMQ.Sdk.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

await client.ConnectAsync();
logger.LogInformation("Connected to licensed KubeMQ instance");
logger.LogInformation("License key is injected via AppHost secret parameter");
logger.LogInformation("See AppHost Program.cs: builder.AddParameter(\"kubemq-license\", secret: true)");

await host.RunAsync();
