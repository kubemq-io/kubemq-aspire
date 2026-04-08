# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-04-04

### Added

- `KubeMQ.Aspire.Hosting` package — provision KubeMQ containers in .NET Aspire AppHost
  - `AddKubeMQ()` extension method for `IDistributedApplicationBuilder`
  - `WithLicenseKey()` to set `KUBEMQ_TOKEN` via parameter or string
  - `WithDataVolume()` to bind persistent storage to `/store`
  - `WithImageTag()` to override the default Docker image tag
  - Three endpoints: gRPC (50000), REST (9090), Dashboard (8080)
  - Connection string injection in `host:port` format
  - Persistent container lifetime by default
- `KubeMQ.Aspire.Client` package — configure `IKubeMQClient` in Aspire service projects
  - `AddKubeMQClient()` with health checks, OpenTelemetry tracing/metrics
  - `AddKeyedKubeMQClient()` for multi-instance keyed DI
  - `KubeMQHealthCheck` with connection state awareness
  - `KubeMQClientSettings` with `Disable*` flags for health checks, tracing, and metrics
  - `ConnectionStringParser` with IPv4/IPv6 support and validation
  - `ConfigurationSchema.json` for IDE IntelliSense
  - Aspire configuration conventions (`Aspire:KubeMQ:Client` section)
