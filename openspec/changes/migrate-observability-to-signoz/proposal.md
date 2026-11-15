# Proposal

## Why

Metrics reach a self-hosted Prometheus/Grafana pair and traces/logs reach only the Aspire Dashboard, so no durable backend exists for all three signals. The project owner chose self-hosted SigNoz as the single long-term backend (issue #549).

## What Changes

- Remove the AppHost OpenTelemetry Collector, its lifecycle hook and config, along with Prometheus and Grafana, so services export straight to the backend named by `OTEL_EXPORTER_OTLP_ENDPOINT`: the Aspire Dashboard in development (Aspire default) and SigNoz when deployed.
- Run SigNoz as a separately managed Foundry stack (`deploy/signoz`), documented in `docs/observability-signoz.md`.

## Capabilities

### New Capabilities

- `observability-backend`: How Hagalaz telemetry reaches its backend.

### Modified Capabilities

None.

## Impact

`Hagalaz.AppHost` (resources removed), `deploy/signoz`, docs, AppHost tests. Service instrumentation is unchanged. This deviates from the issue's "change the existing collector" step because the collector's only purpose was the Prometheus fan-out.

## Scope and Acceptance

- AppHost defines no collector, Prometheus or Grafana resource.
- The backend is selected only by the standard OTLP endpoint variable; no credentials are committed.

## Non-goals and Stop Conditions

- No instrumentation changes, dashboards (#547), admin query API (#548), dual export, or Prometheus history migration.
- Live end-to-end verification against SigNoz is a manual step recorded in the docs.
