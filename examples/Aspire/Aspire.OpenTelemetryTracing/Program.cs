// KubeMQ Aspire — Aspire: OpenTelemetry Tracing
//
// Demonstrates distributed tracing with the Aspire dashboard.
// KubeMQ operations emit traces to the "KubeMQ.Sdk" activity source.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.DisableTracing = false; // default — tracing enabled
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected — tracing enabled. View traces in the Aspire dashboard.");

var stoppingToken = lifetime.ApplicationStopping;

_ = Task.Run(async () =>
{
    try
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            i++;
            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.tracing",
                Body = Encoding.UTF8.GetBytes($"Traced event #{i}")
            }, stoppingToken);
            logger.LogInformation("Sent traced event #{Counter} — check Aspire dashboard for traces", i);
            await Task.Delay(3000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
