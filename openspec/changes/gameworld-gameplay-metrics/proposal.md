# Proposal

## Why

[Issue #546](https://github.com/frankvdb7/Hagalaz/issues/546) adds gameplay activity rates to GameWorld's operational telemetry. Operators need counts that can show an unexpected change in death or skill activity without identifying individual players or inspecting saved progress.

## What Changes

- Count player and NPC transitions into the dead combat state once per life.
- Count resolved kill credit by the credited actor's kind and the victim's kind.
- Count successful Cooking and Fishing actions when they produce cooked food or successfully add a catch to inventory.
- Reuse the GameWorld OpenTelemetry meter registration and document how to graph the counters in SigNoz.
- Verify duplicate death handling, death after respawn, repeated Cooking ticks, and attempts that produce no cooked output. Validate the OpenSpec change and build the solution.

## Capabilities

### New Capabilities

- `gameworld-gameplay-metrics` defines bounded counts at confirmed gameplay transitions.

### Modified Capabilities

None.

## Impact

The scope covers GameWorld combat components, the Cooking and Fishing scripts, their tests, and observability documentation. Each counter needs bounded tags, a verified recording point, and a documented rate graph. Gameplay rules, changes to kill-credit rules, failure analytics, XP totals, player analytics, other skills, and the event migration in #470 are out of scope.
