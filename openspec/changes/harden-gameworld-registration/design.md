## Context

Each GameWorld process currently advertises a world by publishing one `WorldOnlineMessage`. The message has only a logical world ID, and consumers use broker transport addresses as client endpoints. Registration starts from an `ApplicationStarted` fire-and-forget callback, has no renewal or ownership generation, and the default world ID is `1`. The lobby's world-list cache is also independent of status changes, while hosted-service registration causes the bus to stop before the status service can publish its final offline message.

The change must preserve the existing MassTransit status flow, local `WorldInfoStore`, mediator status request, lobby protocol, Aspire health endpoints, and `CharacterDehydrationWorkerService`. The application only needs to make discovery and readiness safe during process failure and replacement; it does not need to replicate game state.

## Goals / Non-Goals

**Goals:**

- Make world configuration fail fast when identity, name, or client endpoint is invalid.
- Give every process generation a stable instance identity, start generation, and renewable lease.
- Ensure all consumers ignore stale offline messages and do not advertise an ambiguous duplicate identity.
- Rebuild a restarted process's local world list through the existing status-request message.
- Keep health readiness, world sign-in admission, status renewal, and shutdown ordering consistent.
- Make world-list cache invalidation and checksums reflect actual world metadata and availability changes immediately.
- Demonstrate two collision-free local Aspire world resources and document the production desired state.

**Non-Goals:**

- Active-active simulation of one logical world.
- Durable storage or replication of NPC, map, event, character-session, or live world state.
- Seamless player migration or automatic selection of another world.
- A new distributed lock, queue, retry framework, or deployment orchestrator.

## Decisions

1. **Typed endpoint configuration.** `WorldOptions` owns a nested advertised endpoint with host and TCP port. The status request consumer copies those values into the status message, and lobby responses use the configured host. Broker source and destination addresses are never used as game-client addresses. Local development supplies explicit values; production must supply them.

2. **Generation-aware status contract.** Online and offline messages carry `InstanceId`, `Generation`, `StartedAt`, `LastSeenAt`, and lease information. `WorldStatusService` creates one identity per process, publishes the full online snapshot, renews it periodically, and retries failed publication. The generation is compared before replacing an observed registration; an offline message removes only the matching instance and generation.

3. **One observed-registration owner.** `WorldRegistrationStore` is the single owner for the local view of all live generations. It retains the full latest online snapshot per `(WorldId, InstanceId)`, exposes whether the local generation is conflicted, expires leases, and returns the surviving snapshot after an offline/expiry transition. World-status consumers apply those transitions to `WorldInfoService`; no parallel world-list store is introduced.

4. **Per-process broadcast subscriptions.** The three GameWorld status consumers are excluded from MassTransit's shared `ConfigureEndpoints` registration. Each process binds them to a unique non-durable, auto-delete receive endpoint named with its `InstanceId`, so status requests and online/offline publications are copied to every process-local registration store rather than load-balanced between replicas.

5. **Readiness and initialization.** `WorldLifecycleState` owns initialization, application-started, registration, conflict, and stopping flags. The existing `StartupServiceExecutor` optionally reports completion/failure through the small `IStartupTaskState` boundary. The world health check is ready only when initialization and endpoint startup completed, the local generation registered successfully, and no other live generation owns the same world ID. The generic `self` check remains tagged `live`, so `/alive` stays independent.

6. **Reconstruction through the existing message.** After its first successful registration, a GameWorld publishes `WorldStatusRequest`. Every GameWorld status consumer responds with its current status and publishes that status. This reuses the existing MassTransit message and makes a restarted lobby/world converge without waiting for unrelated processes to restart. Duplicate same-generation messages are idempotent.

7. **Cache invalidation at the existing owner.** `WorldInfoService` keeps the existing HybridCache key but removes it whenever world metadata, endpoint, or online availability changes. It orders snapshots deterministically before creating a new checksum. Character-count-only changes continue to use the separate character-info update path and do not cause a full metadata checksum churn.

8. **Shutdown ordering through registration order plus an early stop flag.** An `ApplicationStopping` callback immediately removes readiness and world-sign-in admission. MassTransit is registered before `WorldStatusService`, and `WorldStatusService` before `CharacterDehydrationWorkerService`, producing stop order: character flush, generation-matching offline publication, then bus shutdown. The existing worker remains the only durable character handoff path.

9. **Contacts generation and lease handling.** `WorldSessionStore` owns all observed `(WorldId, InstanceId)` sessions, expires missed leases on a periodic timer, and reports the surviving generation after an offline transition. `WorldStatusConsumer` performs contact cleanup only when no live generation remains, preserving graceful cleanup while preventing a delayed old offline event or a surviving replacement from signing out contacts.

10. **Legacy client endpoint compatibility.** The 742 world-list and lobby response wire contracts carry the world host but no per-world port, and the client uses its configured TCP port. The internal status model still validates and records the advertised port, while local Aspire config binds both worlds to the existing client port `443` on distinct loopback hosts (`127.0.0.1` and `127.0.0.2`).

## Risks / Trade-offs

- **[Risk]** A process that is partitioned from RabbitMQ cannot prove that no duplicate is alive. → Readiness fails when renewal fails, leases expire locally, and any observed conflicting live generation makes the local health check fail; the deployment remains responsible for one desired process per identity.
- **[Risk]** A restarted lobby has no durable world-list snapshot. → It immediately publishes the existing status request and each live world republishes a complete status; stale entries are lease-expired locally.
- **[Risk]** Existing clients only encode a world host string. → The legacy protocol continues to encode the client-compatible host field, and each deployment must configure the advertised port to match the client's existing TCP port; local Aspire proves the two-host/same-port topology.
- **[Risk]** Existing deployments do not provide an identity or endpoint. → Development launch/Aspire configuration supplies explicit local values; production startup validation fails clearly until deployment configuration is updated.

## Migration Plan

1. Deploy the code with unique `HAGALAZ_WORLD_ID` and `HAGALAZ_World__AdvertisedEndpoint__Host/Port` for every GameWorld workload.
2. Roll each world one at a time so the old process publishes/flushes before its replacement is started; readiness probes prevent routing an unregistered process.
3. Configure the orchestrator for one serving replica per world identity, automatic restart, `/health` readiness, and `/alive` liveness.
4. Rollback is code-version based: keep the same unique identity and endpoint settings. A rollback process still emits generation-aware status, and its replacement will ignore the newer process's stale offline event by generation.

## Open Questions

- The legacy 742 client protocol has no field for a per-world port in its world-list payload. The application will carry and validate the port in status/internal models while preserving the existing host-string wire format; a future protocol revision can expose the port explicitly if required.
