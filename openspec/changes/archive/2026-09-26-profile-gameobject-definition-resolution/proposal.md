## Why

Real region-load traces measured about 5.5 seconds inside bulk GameObject-definition resolution, while correlated definition SQL accounted for only about 178 ms. The current telemetry cannot distinguish cache, archive decoding, EF materialization, or composition time, so another optimization would be guesswork.

## What Changes

- Add bounded per-batch activities and aggregate timings for cache-key preparation, HybridCache execution, repository work, archive/type-provider resolution, and definition/dictionary composition.
- Record whether the HybridCache factory ran, its elapsed time, and the time between factory completion and cache return.
- Record counts and timings only. Do not record object IDs or create activities per definition.
- Preserve runtime behavior and leave the live Aspire restart and client reproduction to the user.

## Capabilities

### New Capabilities

None. This change adds diagnostics and does not change system behavior.

### Modified Capabilities

None.

This change sets `skip_specs: true` because it introduces no spec-level behavior requirement.

## Impact

- `Hagalaz.Services.GameWorld` bulk cache, resolution, and repository code, plus the GameObject-definition archive codec if needed for phase attribution.
- Existing `Hagalaz.Services.GameWorld` OpenTelemetry source and EF command spans; no new package or cache mechanism.
- Focused GameWorld cache, service, and region-loader tests, the full GameWorld test suite, and the GameWorld build.

Non-goals: changing cache granularity, archive-provider behavior, NPC viewport semantics, region scheduling, login ordering, persistence, or any client protocol.
