# Proposal

## Why

Traces and logs reach only the Aspire Dashboard and metrics reach a self-hosted Prometheus/Grafana pair, so no durable backend exists for all three signals. The project owner chose self-hosted SigNoz as the single long-term backend (issue #549).

## What Changes

- The existing AppHost OpenTelemetry Collector exports metrics, traces and logs to a configurable SigNoz OTLP endpoint, keeping the Aspire export.
- SigNoz runs as a separately managed Foundry stack (`deploy/signoz`), documented in `docs/observability-signoz.md`.
- The Prometheus and Grafana AppHost resources, collector exporter, and their config assets are removed.

## Capabilities

### New Capabilities

- `observability-backend`: Where Hagalaz collector telemetry is delivered.

### Modified Capabilities

None.

## Impact

`Hagalaz.AppHost` (resources, collector config), `deploy/signoz`, docs, AppHost tests. Service instrumentation is unchanged.

## Scope and Acceptance

- Collector pipelines for metrics, traces and logs include the Aspire and SigNoz exporters.
- AppHost no longer defines Prometheus or Grafana.
- Endpoint and TLS are configurable; no credentials are committed.

## Non-goals and Stop Conditions

- No instrumentation changes, dashboards (#547), admin query API (#548), or data migration of Prometheus history.
- Live end-to-end verification against a running SigNoz is a manual step recorded in the docs.
