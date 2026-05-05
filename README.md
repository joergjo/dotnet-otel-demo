# .NET OpenTelemetry Demo

A .NET demo application showcasing OpenTelemetry instrumentation with multiple telemetry backends — including local observability stacks, [Azure Monitor's native OTLP ingestion](https://learn.microsoft.com/en-us/azure/azure-monitor/containers/opentelemetry-protocol-ingestion), and [Honeycomb](https://www.honeycomb.io/). The app uses the [Azure Monitor OpenTelemetry Distro](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable) as well as the [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) for flexible telemetry routing.

## Overview

The application is an enhanced version of the OpenTelemetry [dice roller](https://opentelemetry.io/docs/languages/net/getting-started/) introductory sample. It exposes a single HTTP endpoint that rolls a dice, while emitting traces, metrics, and logs via OpenTelemetry.

The demo can run with three different telemetry backends:

| Backend | Compose file | Description |
|---|---|---|
| **Local** | `compose.local.yaml` | Jaeger for traces, Prometheus for metrics |
| **Azure Monitor** | `compose.azure.yaml` | Azure Monitor OTLP ingestion via the OTel Collector |
| **Honeycomb** | `compose.honeycomb.yaml` | Honeycomb via the OTel Collector |

> [!NOTE]
> The local backend runs entirely on your machine. Azure Monitor and Honeycomb require real cloud accounts and credentials.

## Branches

This repository contains two branches that demonstrate different instrumentation approaches:

- **`main`** — Uses .NET's native diagnostic APIs (`Activity`, `ActivitySource`) for tracing
- **`shim`** — Uses the [OpenTelemetry .NET Shim API](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Api) (`Tracer`, `Span`) which follows OpenTelemetry's cross-language terminology

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/)
- For Azure Monitor: An Azure subscription with [Application Insights and OpenTelemetry support enabled](https://learn.microsoft.com/en-us/azure/azure-monitor/containers/opentelemetry-protocol-ingestion#set-up-otlp-data-ingestion)
- For Honeycomb: A [Honeycomb](https://www.honeycomb.io/) account and API key

## Getting started

### Local backend (Jaeger + Prometheus)

```bash
docker compose -f compose.yaml -f compose.local.yaml up
```

Once running, access:
- **Application:** http://localhost:8080/rolldice
- **Jaeger UI:** http://localhost:16686
- **Prometheus UI:** http://localhost:9090

### Azure Monitor

Set the required environment variables:

**Bash/Zsh:**

```bash
export CLIENT_ID='<your-service-principal-client-id>'
export CLIENT_SECRET='<your-service-principal-client-secret>'
export TENANT_ID='<your-azure-tenant-id>'
export TRACES_ENDPOINT='<your-azure-monitor-traces-endpoint>'
export METRICS_ENDPOINT='<your-azure-monitor-metrics-endpoint>'
export LOGS_ENDPOINT='<your-azure-monitor-logs-endpoint>'
```

**PowerShell:**

```powershell
$env:CLIENT_ID='<your-service-principal-client-id>'
$env:CLIENT_SECRET='<your-service-principal-client-secret>'
$env:TENANT_ID='<your-azure-tenant-id>'
$env:TRACES_ENDPOINT='<your-azure-monitor-traces-endpoint>'
$env:METRICS_ENDPOINT='<your-azure-monitor-metrics-endpoint>'
$env:LOGS_ENDPOINT='<your-azure-monitor-logs-endpoint>'
```

Then start the application:

```bash
docker compose -f compose.yaml -f compose.azure.yaml up
```

### Honeycomb

Set the required environment variables:

**Bash/Zsh:**

```bash
export HONEYCOMB_API_KEY='<your-honeycomb-api-key>'
export HONEYCOMB_DATASET='<your-dataset-name>'
```

**PowerShell:**

```powershell
$env:HONEYCOMB_API_KEY='<your-honeycomb-api-key>'
$env:HONEYCOMB_DATASET='<your-dataset-name>'
```

Then start the application:

```bash
docker compose -f compose.yaml -f compose.honeycomb.yaml up
```

## Usage

Roll a dice:

```bash
curl http://localhost:8080/rolldice
```

Roll a dice as a named player:

```bash
curl http://localhost:8080/rolldice/Alice
```

## Project structure

```
├── DiceRoller/
│   ├── Program.cs           # Application entry point and OTel configuration
│   ├── Telemetry.cs         # Telemetry constants, meters, and counters
│   ├── Dockerfile           # Multi-stage container build
│   └── DiceRoller.csproj    # Project file (.NET 10)
├── config/
│   ├── collector.yaml           # OTel Collector config (base)
│   ├── collector.azure.yaml     # OTel Collector config (Azure Monitor)
│   ├── collector.honeycomb.yaml # OTel Collector config (Honeycomb)
│   └── prometheus.yml           # Prometheus scrape config
├── compose.yaml             # Base Docker Compose (app + collector)
├── compose.local.yaml       # Local backend overlay (Jaeger + Prometheus)
├── compose.azure.yaml       # Azure Monitor overlay
└── compose.honeycomb.yaml   # Honeycomb overlay
```

## Azure Monitor Distro

In addition to routing telemetry through the OTel Collector, the application also supports the [Azure Monitor OpenTelemetry Distro](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore) for direct ingestion into Application Insights. Use the `UseOtlpExport` command line switch or environment variable to switch from native OTLP ingestion to the native Application Insights data path.  

### Zsh or Bash
```bash
# Use command line switch
dotnet run -- --UseOtlpExport=false

#  Use environment variable
UseOtlpExport=false dotnet run
```

### PowerShell

```pwsh
# Use command line switch
dotnet run -- --UseOtlpExport=false

$env:UseOtlpExport = "false"
dotnet run
```

When you are using the Azure Monitor Distor, you can use an Application Insights resource either with or  without OpenTelemetry support enabled.