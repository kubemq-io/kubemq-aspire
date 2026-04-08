# KubeMQ .NET Aspire Integration

.NET Aspire integration for [KubeMQ](https://kubemq.io) message broker. Provides two NuGet packages following the standard Aspire two-package model:

| Package | NuGet | Description |
|---------|-------|-------------|
| `KubeMQ.Aspire.Hosting` | [![NuGet](https://img.shields.io/nuget/v/KubeMQ.Aspire.Hosting)](https://www.nuget.org/packages/KubeMQ.Aspire.Hosting) | Provision KubeMQ containers in the Aspire AppHost |
| `KubeMQ.Aspire.Client` | [![NuGet](https://img.shields.io/nuget/v/KubeMQ.Aspire.Client)](https://www.nuget.org/packages/KubeMQ.Aspire.Client) | Configure `IKubeMQClient` with health checks, OpenTelemetry, and keyed DI |

## Installation

### AppHost project

```bash
dotnet add package KubeMQ.Aspire.Hosting
```

### Service project

```bash
dotnet add package KubeMQ.Aspire.Client
```

## Usage

### AppHost (Program.cs)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var kubemqKey = builder.AddParameter("kubemq-key", secret: true);

var messaging = builder.AddKubeMQ("messaging")
    .WithLicenseKey(kubemqKey)
    .WithDataVolume();

builder.AddProject<Projects.MyWebApi>("webapi")
    .WithReference(messaging)
    .WaitFor(messaging);

builder.Build().Run();
```

### Service project (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddKubeMQClient("messaging");

var app = builder.Build();
app.Run();
```

### Keyed services (multiple KubeMQ instances)

```csharp
// AppHost
var orders = builder.AddKubeMQ("orders");
var notifications = builder.AddKubeMQ("notifications");

builder.AddProject<Projects.MyService>("service")
    .WithReference(orders)
    .WithReference(notifications);

// Service
builder.AddKeyedKubeMQClient("orders");
builder.AddKeyedKubeMQClient("notifications");

// Inject
public class OrderService([FromKeyedServices("orders")] IKubeMQClient client) { }
```

## Hosting API

| Method | Description |
|--------|-------------|
| `AddKubeMQ(name, grpcPort?)` | Add a KubeMQ container resource |
| `WithLicenseKey(key)` | Set the `KUBEMQ_TOKEN` environment variable |
| `WithDataVolume(name?)` | Bind a persistent volume to `/store` |
| `WithImageTag(tag)` | Override the Docker image tag (default: `latest`) |

### Endpoints

| Name | Target Port | Protocol |
|------|-------------|----------|
| gRPC | 50000 | TCP |
| REST | 9090 | HTTP |
| Dashboard | 8080 | HTTP |

## Client API

| Method | Description |
|--------|-------------|
| `AddKubeMQClient(connectionName, configureSettings?, configureOptions?)` | Register `IKubeMQClient` singleton |
| `AddKeyedKubeMQClient(name, configureSettings?, configureOptions?)` | Register keyed `IKubeMQClient` |

### Configuration

Settings can be configured via `appsettings.json`:

```json
{
  "Aspire": {
    "KubeMQ": {
      "Client": {
        "DisableHealthChecks": false,
        "DisableTracing": false,
        "DisableMetrics": false,
        "HealthCheckTimeout": "00:00:05",
        "AuthToken": null,
        "ClientId": null
      }
    }
  }
}
```

Or via the settings delegate:

```csharp
builder.AddKubeMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = true;
    settings.AuthToken = "my-token";
});
```

### Health Checks

The integration registers health checks with tags `["ready", "live"]` that report:

| Connection State | Health Status |
|-----------------|---------------|
| Ready | Healthy (with server info) |
| Reconnecting | Degraded |
| Connecting | Unhealthy |
| Idle | Unhealthy |
| Closed | Unhealthy |

### OpenTelemetry

Tracing and metrics are enabled by default using the SDK's built-in instrumentation:

- **Tracing:** `AddSource("KubeMQ.Sdk")`
- **Metrics:** `AddMeter("KubeMQ.Sdk")`

Disable via `DisableTracing` or `DisableMetrics` settings.

## Requirements

- .NET 8.0 or .NET 9.0
- .NET Aspire 9.0+
- KubeMQ license key (set via `WithLicenseKey()`)
- Docker (for local development with Aspire)

## License

Apache 2.0 — see [LICENSE](LICENSE) for details.
