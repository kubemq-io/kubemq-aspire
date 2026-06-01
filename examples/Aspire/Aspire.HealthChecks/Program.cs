// KubeMQ Aspire — Aspire: Health Checks
//
// Demonstrates automatic health check registration.
// Access /health (readiness) and /alive (liveness) endpoints.

using KubeMQ.Sdk.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddKubeMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = false; // default
    settings.HealthCheckTimeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Map health endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

var client = app.Services.GetRequiredService<IKubeMQClient>();
await client.ConnectAsync();

app.MapGet("/", () => "KubeMQ Health Check Example — try /health and /alive");

app.Logger.LogInformation("Health check endpoints: /health (ready), /alive (live)");

app.Run();
