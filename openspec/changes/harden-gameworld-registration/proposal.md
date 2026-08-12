## Why

GameWorld currently publishes a one-shot, generation-blind status using broker addresses and a silent world-ID fallback. A failed or duplicated process can therefore remain discoverable, invalidate a replacement with a stale offline event, or advertise an endpoint that game clients cannot reach. This change hardens the existing registration and lobby lifecycle so each configured world can be restarted safely in a clustered deployment.

## What Changes

- Require and validate a positive world identity, non-empty world name, client-reachable advertised host, and TCP port at GameWorld startup; remove the production fallback to world ID `1`.
- Add a typed advertised endpoint and use it for status messages, lobby sign-in responses, and world-list entries instead of MassTransit addresses or `127.0.0.1`.
- Extend online/offline status with a unique process instance, generation/start timestamp, and lease expiry; publish the status with retry and periodic renewal.
- Track observed registrations locally, detect conflicting live generations, expire missed leases, and ignore stale offline events.
- Reconstruct the world list by requesting current status from all live worlds when a world/lobby starts.
- Make the shared world-list cache content-addressed so metadata and checksum identity remain correct across process restarts.
- Add a world-aware readiness check while retaining `/alive` as liveness, and reject new world sign-ins after shutdown readiness is removed.
- Preserve the existing character snapshot shutdown flush and order readiness removal, durable flush, generation-matching offline publication, and bus shutdown.
- Represent two independently configured worlds and proxyless, collision-free TCP endpoints in the local Aspire configuration, including the legacy client's fixed-port constraint, and document the one-serving-instance-per-identity production shape.
- Add focused MSTest coverage for validation, endpoint routing, renewal/expiry, stale generations, duplicate identities, reconstruction, cache freshness, contacts cleanup, per-process broadcast subscriptions, legacy two-world connection routing, and shutdown ordering.

## Capabilities

### New Capabilities

- `clustered-world-registration`: Generation-aware world registration, lease expiry, endpoint advertisement, readiness, reconstruction, and shutdown ordering.

### Modified Capabilities

- None.

## Impact

- Affected projects: `Hagalaz.Game.Configuration`, `Hagalaz.Game.Messages`, `Hagalaz.Services.GameWorld`, `Hagalaz.Services.Contacts`, `Hagalaz.ServiceDefaults`, and `Hagalaz.AppHost`.
- Existing status message contracts gain generation and endpoint fields; producers and consumers in this repository are migrated together.
- No new external package, durable world-state store, active-active simulation, wire-protocol port extension, or player migration mechanism is introduced.
- Production deployment documentation will define unique `HAGALAZ_WORLD_ID` and endpoint settings, restart policy, single serving instance, and `/health`/`/alive` probes.
