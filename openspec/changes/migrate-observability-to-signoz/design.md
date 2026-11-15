# Design

- Owner: the AppHost collector is the only telemetry fan-out; services keep exporting to it.
- SigNoz is an external, Foundry-managed stack, because its internal ClickHouse/Keeper/Postgres services must not be hand-modeled as Aspire resources.
- Each OTLP exporter has its own queue and retry, so one unavailable backend does not block the other; no extra queue is added.
- Rejected: an Aspire-managed SigNoz pseudo-stack (fragile, unsupported); dual Prometheus+SigNoz retention (out of scope).
