# Observability with self-hosted SigNoz

Hagalaz services emit OpenTelemetry metrics, traces and logs (`Hagalaz.ServiceDefaults`). The AppHost
OpenTelemetry Collector forwards all three signals to the Aspire Dashboard (developer diagnostics) and to
a self-hosted [SigNoz](https://signoz.io) stack. SigNoz replaces the former Prometheus + Grafana containers.

```text
services --OTLP--> AppHost collector --+--> Aspire Dashboard
                                       +--> SigNoz ingester (4317/4318) -> ClickHouse -> SigNoz UI/alerts/API
```

SigNoz is **optional** for local development: it is a separately managed stack, not an Aspire resource. If it
is not running, the collector keeps exporting to Aspire and retries SigNoz in the background (each exporter has its own queue).

## Setup

Requires Linux, macOS, or Windows via WSL 2 with native Docker Engine, and at least 4 GB for Docker.

```bash
curl -fsSL https://signoz.io/foundry.sh | bash   # installs foundryctl
cd deploy/signoz
foundryctl cast -f casting.yaml                  # gauge + forge + start
```

UI: <http://localhost:8080> (create the first admin user there; no credentials are committed).
OTLP ingestion: `4317` (gRPC) and `4318` (HTTP), published by the SigNoz ingester.

`deploy/signoz/casting.yaml` pins the SigNoz image. The ingester and ClickHouse images are Foundry defaults;
pin them via `spec.patches` once a tested set is recorded. Never edit `pours/`; re-run `foundryctl cast` after
changing the casting. Volumes persist across restarts.

Start the AppHost afterwards. The collector reaches SigNoz at `host.docker.internal:4317`; override with AppHost configuration:

| Key | Default | Meaning |
|---|---|---|
| `SigNoz:OtlpEndpoint` | `host.docker.internal:4317` | gRPC OTLP endpoint of the SigNoz ingester |
| `SigNoz:OtlpInsecure` | `true` | `false` enables TLS (production-like). Never expose 4317/4318/ClickHouse publicly. |

Self-hosted SigNoz needs no ingestion key.

## Stop, teardown, rollback

```bash
cd deploy/signoz/pours/deployment
docker compose stop        # stop, keep data
docker compose down        # remove containers, keep volumes
docker compose down -v     # DESTROYS all telemetry history
```

Prometheus and Grafana were removed with no data migration (clean cutover); old local Prometheus data is not
carried over. To roll back, check out the commit before this change.

## Querying

The SigNoz Metrics API (`POST /api/v5/query_range` on the UI port) is the backend for the admin query adapter (#548).
Dashboards and alerts belong to #547.

## Identity

Aspire sets `service.name` per resource (`hagalaz-services-gameworld-1` vs `-2`) and `service.instance.id`,
so worlds are distinguishable in SigNoz without extra attributes.

## Not yet verified

A live end-to-end run (SigNoz UI, world separation, `query_range`, log/trace correlation, resource usage and
retention figures) has not been exercised. Record observed values here after a live run.
