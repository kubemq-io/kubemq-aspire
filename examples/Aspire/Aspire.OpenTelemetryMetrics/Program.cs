// KubeMQ Aspire — Aspire: OpenTelemetry Metrics
//
// Demonstrates metrics collection from the "KubeMQ.Sdk" meter.
// Metrics are exported to the Aspire dashboard via OTLP.

using System.Text;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.DisableMetrics = false; // default — metrics enabled
});

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("Connected — metrics enabled. View metrics in the Aspire dashboard.");

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
                Channel = "events.metrics",
                Body = Encoding.UTF8.GetBytes($"Metered event #{i}")
            }, stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
