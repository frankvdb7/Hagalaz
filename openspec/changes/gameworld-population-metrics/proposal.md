# Proposal

## Why

`CharacterStore`, `NpcStore` and `MapRegionService` own the registered creatures and the active/idle region state, but none of it is observable as a metric, and the tick does not report how many regions it processed.

**Goal:** the existing `Hagalaz.Services.GameWorld` meter exposes registered characters, registered NPCs, ready regions, idle regions and the ready regions processed per tick.

## What Changes

- Add four gauges: `hagalaz.gameworld.characters.registered` (`{character}`), `hagalaz.gameworld.npcs.registered` (`{npc}`), `hagalaz.gameworld.regions.ready` (`{region}`), `hagalaz.gameworld.regions.idle` (`{region}`).
- Add histogram `hagalaz.gameworld.tick.regions` (`{region}`), one sample per tick.
- Counts are published by the owners after a successful transition: the stores after a successful add/remove, `MapRegionService` after a successful suspend, resume or idle removal, and the worker with the ready-region count it actually ticks. Gauge callbacks only read published integers.
- `characters.registered` counts characters held by the character store. World admission registers a character before its session is committed, so it is not the authenticated player population.
- `regions.ready` is the ready-region count of the most recent tick, so it is zero until the first tick.

## Non-goals

Per-player, NPC, region or dimension tags, login counts, Raido connection counts, a new population cache.

## Stop conditions

Stop if the work needs a new store scan, lock, or region enumeration from a gauge callback.
