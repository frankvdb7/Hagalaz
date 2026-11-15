## ADDED Requirements

### Requirement: Services export directly to the configured backend

Hagalaz services SHALL export metrics, traces and logs over OTLP to the endpoint in `OTEL_EXPORTER_OTLP_ENDPOINT`, with no intermediate collector in the AppHost.

#### Scenario: Local development
- **WHEN** the AppHost runs services
- **THEN** telemetry goes to the Aspire Dashboard and no collector resource exists

#### Scenario: Deployed
- **WHEN** the endpoint variable points at SigNoz
- **THEN** metrics, traces and logs are delivered to SigNoz

### Requirement: No Prometheus or Grafana runtime

The AppHost SHALL NOT define Prometheus or Grafana resources.

#### Scenario: AppHost model
- **WHEN** the AppHost model is built
- **THEN** it contains no `prometheus`, `grafana` or `otelcollector` resource
