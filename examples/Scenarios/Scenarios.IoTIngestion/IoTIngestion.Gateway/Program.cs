// KubeMQ Aspire — Scenario: IoT Ingestion — Gateway
//
// Simulates sensor data and publishes to PubSub channels.

using System.Text;
using System.Text.Json;
using KubeMQ.Sdk.Client;
using KubeMQ.Sdk.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKubeMQClient("messaging");

var host = builder.Build();
var client = host.Services.GetRequiredService<IKubeMQClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

await client.ConnectAsync();
logger.LogInformation("IoT Gateway started — simulating sensor data");

var stoppingToken = lifetime.ApplicationStopping;
var random = new Random();

_ = Task.Run(async () =>
{
    try
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            i++;
            var temperature = 20 + random.NextDouble() * 30;
            var data = JsonSerializer.Serialize(new { sensorId = "temp-01", value = temperature, ts = DateTime.UtcNow });

            await client.SendEventAsync(new EventMessage
            {
                Channel = "events.iot.sensors",
                Body = Encoding.UTF8.GetBytes(data)
            }, stoppingToken);

            logger.LogInformation("[Gateway] Sensor reading #{I}: {Temp:F1}C", i, temperature);
            await Task.Delay(1000, stoppingToken);
        }
    }
    catch (OperationCanceledException) { }
}, stoppingToken);

await host.RunAsync();
