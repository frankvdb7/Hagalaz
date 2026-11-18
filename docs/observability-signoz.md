# Observability with self-hosted SigNoz

Hagalaz services emit OpenTelemetry metrics, traces and logs (`Hagalaz.ServiceDefaults`) through the standard OTLP
exporter, which is enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. There is no collector: each service exports
directly to one backend, chosen only by that variable.

- **Local development:** Aspire sets the variable to the Aspire Dashboard for every project. Nothing else to run.
- **Deployed:** set `OTEL_EXPORTER_OTLP_ENDPOINT` on each service to the SigNoz ingester
  (`http://<host>:4317` for gRPC, or `http://<host>:4318` with `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`).
  Use TLS in production-like environments and never expose 4317/4318 or ClickHouse publicly.
  Self-hosted SigNoz needs no ingestion key. Each service reports its own `service.name`
  (for example `hagalaz-services-gameworld-1` vs `-2` under Aspire) and `service.instance.id`.

SigNoz replaces the former Prometheus + Grafana containers, which were removed without data migration (clean cutover).

## SigNoz setup

Requires Linux, macOS, or Windows via WSL 2 with native Docker Engine, and at least 4 GB for Docker.

```bash
curl -fsSL https://signoz.io/foundry.sh | bash   # installs foundryctl
cd deploy/signoz
foundryctl cast -f casting.yaml                  # gauge + forge + start
```

UI: <http://localhost:8080> (create the first admin user there; no credentials are committed).
`deploy/signoz/casting.yaml` pins the SigNoz image. The ingester and ClickHouse images are Foundry defaults;
pin them via `spec.patches` once a tested set is recorded. Never edit `pours/`. Volumes persist across restarts.

To try SigNoz locally, override the endpoint for a service (for example with the
`OTEL_EXPORTER_OTLP_ENDPOINT` environment variable); it then no longer reports to the Aspire Dashboard.

## Stop, teardown, rollback

```bash
cd deploy/signoz/pours/deployment
docker compose stop        # stop, keep data
docker compose down        # remove containers, keep volumes
docker compose down -v     # DESTROYS all telemetry history
```

To roll back, check out the commit before this change.

## Querying

The SigNoz Metrics API (`POST /api/v5/query_range` on the UI port) is the backend for the admin query adapter (#548).
Dashboards and alerts belong to #547.

### GameWorld gameplay counters

| Metric | Counts | Measurement tag |
| --- | --- | --- |
| `hagalaz.gameworld.combat.death` | One death per transition into the dead combat state | `actor.kind=player\|npc` |
| `hagalaz.gameworld.combat.kill.credited` | One resolved kill credit per death with a player or NPC killer | `killer.kind` and `victim.kind`, each `player` or `npc` |
| `hagalaz.gameworld.skill.action.completed` | One successful Cooking or Fishing action | `skill=cooking\|fishing` |

`CharacterCombat.OnDeath` and `NpcCombat.OnDeath` record the death when they first set `IsDead` for that life.
Repeated handling while the actor is dead and delayed kill or cleanup notifications add no counts. Spawning
clears the flag so a later death counts again.

The original delayed death task records kill credit when `GetKiller` resolves the credited actor, before
notifying the killer and victim. The game can credit a familiar's summoner. Unresolved killers produce
only a death count. Kill credit measures attribution under the existing combat rules.

`CookingTask.PerformTickImpl` records the action after replacing the raw item with cooked food. An
interruption, burned food, or missing input produces no completion count. The catch callback in
`FishingSkillService.TryFish` records one action when inventory accepts the first catch from that attempt.
An attempt can yield several fish while contributing one completion. Reconnect resumes the same live
character without replaying combat or skill actions. The counters carry only the measurement tags above.

In a SigNoz time-series panel, select the death metric, filter `service.name` to the GameWorld services you
want to monitor, and set the step to five minutes. Use **Rate** within each series, then **Sum** across
series grouped by `actor.kind`. Applying rate before sum handles resets on individual instances and gives
two series in deaths per second. For kill credit, group by both `killer.kind` and `victim.kind` to see
patterns such as player-on-player and player-on-NPC attribution. For skill activity, group by `skill`
to compare completed actions per second. [SigNoz metrics query guide](https://signoz.io/docs/metrics-management/querying-metrics/)

## Not yet verified

A live run against SigNoz (UI data, world separation, `query_range`, log/trace correlation, resource usage and
retention figures) has not been exercised. Record observed values here after a live run.
