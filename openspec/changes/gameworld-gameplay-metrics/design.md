# Design

## Context

GameWorld owns combat state and kill-credit resolution. The dynamically loaded scripts plugin owns Cooking and Fishing. `Hagalaz.ServiceDefaults` already subscribes to the `Hagalaz.Services.GameWorld` meter name for OTLP export.

## Goals / Non-Goals

Record measurements alongside each gameplay owner's existing mutation. Keep the counters independent of save, replay, and reconnect logic.

## Decisions

### Count the victim's combat state transition

`CharacterCombat.OnDeath` and `NpcCombat.OnDeath` already set `IsDead` before rendering or queuing cleanup. Route those writes through a small protected `CreatureCombat` method that records a death via `GameWorldMetrics` only on the first transition to `true`. Derive `actor.kind` from the owner type. The method records the transition without changing whether the rest of each caller runs. Existing `OnSpawn` resets the flag for the next life.

The outer creature methods already skip death handling for a dead actor. The transition check also covers repeated or reentrant calls that reach the combat component. Hit callbacks can run for nonlethal hits. `CreatureDiedEvent` is sent before the state changes, and delayed `OnKilledBy` and `OnTargetKilled` can both publish a kill notification for one death. Neither death nor kill credit is recorded by a notification subscriber. Deaths without an attributed killer qualify when they enter the dead combat state.

### Count kill credit where GameWorld resolves it

Capture whether `MarkDead` made the transition when constructing the existing delayed death task. Both actors call `CompleteCombatDeath` to resolve credit, notify the victim and killer, and clear attacker state. This runs after the lethal hit's damage contribution is recorded. `GameWorldMetrics` maps actor types to the bounded tags used by both counters. Tasks queued by repeated direct death calls retain their gameplay behavior and cannot emit kill credit. The metric follows the existing credit rules, including summon ownership.

### Count each cooked product at inventory replacement

`CookingTask.PerformTickImpl` verifies the cooking object and raw inventory slot, then synchronously calls `Inventory.ReplaceItem`. Record once after that call succeeds on the non-burned branch, before the existing XP award. This counts one successful action even if a subsequent XP or notification step fails. Keep the instrument private to the scripts plugin under the existing exported meter name so the plugin needs no reference to the host assembly.

The creature task scheduler owns tick execution. The task advances its tick and batch counts, consumes the raw item on success, and skips callbacks after cancellation or fault. A later tick must make another replacement to produce another count. Session reattachment reuses the live character and does not replay inventory or combat mutations. There is no metric retry or reconciliation job. Generic hooks on task completion, inventory events, or XP would conflate cancellation, unrelated item changes, or different XP sources with the actual Cooking action.

### Count Fishing when inventory accepts a catch

`FishingSkillService.TryFish` owns the catch callback. Record at its first successful inventory insertion per invocation. Further fish in that same attempt share the count. An exhausted spot, a cancelled task, or a loot roll with no accepted items records nothing. Share the private scripts meter between the two skills.

### Graph activity rates in SigNoz

| Counter | Unit | Measurement tag |
| --- | --- | --- |
| `hagalaz.gameworld.combat.death` | `{death}` | `actor.kind` = `player` or `npc` |
| `hagalaz.gameworld.combat.kill.credited` | `{kill}` | `killer.kind` and `victim.kind` = `player` or `npc` |
| `hagalaz.gameworld.skill.action.completed` | `{action}` | `skill` = `cooking` or `fishing` |

In a time-series panel, graph the per-second rate of each counter over a five-minute window. Calculate rates per source series before summing so process restarts do not appear as activity spikes. Group deaths by `actor.kind`, kill credit by `killer.kind` and `victim.kind`, and skill activity by `skill`. These graphs describe the selected events across the chosen GameWorld instances.

## Risks / Trade-offs

Counts are process-local operational telemetry. The tests should capture actual measurements at the gameplay callsites, isolate process-wide metric listeners from parallel tests, and check each sample's value and full tag set. Export outages can lose measurements. Additional skills should be considered individually once their successful action boundary is confirmed.

## Migration Plan

Deploy with the existing OTLP configuration. Reverting the change removes the counters and requires no data migration.
