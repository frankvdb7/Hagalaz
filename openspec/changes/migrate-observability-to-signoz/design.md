# Design

- Owner: each service exports through the existing `UseOtlpExporter` in ServiceDefaults; the endpoint variable is the single selector.
- SigNoz is an external, Foundry-managed stack, because its ClickHouse/Keeper/Postgres services must not be hand-modeled as Aspire resources.
- Rejected: keeping a collector for dual export (extra moving part with no current requirement); an Aspire-managed SigNoz pseudo-stack (fragile, unsupported).
