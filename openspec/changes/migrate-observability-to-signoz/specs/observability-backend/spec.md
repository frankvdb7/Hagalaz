## ADDED Requirements

### Requirement: Collector delivers all signals to SigNoz

The AppHost OpenTelemetry Collector SHALL export metrics, traces and logs to the configured SigNoz OTLP endpoint and to the Aspire Dashboard.

#### Scenario: Pipelines include both exporters
- **WHEN** the collector configuration is loaded
- **THEN** the metrics, traces and logs pipelines each export to Aspire and to SigNoz

#### Scenario: SigNoz unavailable
- **WHEN** the SigNoz endpoint is unreachable
- **THEN** the Aspire export continues independently

### Requirement: No Prometheus or Grafana runtime

The AppHost SHALL NOT define Prometheus or Grafana resources.

#### Scenario: AppHost model
- **WHEN** the AppHost model is built
- **THEN** it contains the collector and no `prometheus` or `grafana` resource
