# Thieving — revision 742

**Repository assessment:** Pickpocket, stalls and wall-safe tasks exist. Existing files are entry points, not evidence of feature completeness.

## Historical scope

NPC pickpocketing, stall theft, rewards, cooldowns, stun/retaliation, detection chance and area restrictions.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Thieving/StandardPickPocketTask.cs`
- `Hagalaz.Game.Scripts/Skills/Thieving/StandardStealTask.cs`
- `Hagalaz.Game.Scripts/Skills/Thieving/PickPockatableNPC.rsnpc.cs`
- `Hagalaz.Game.Scripts/Skills/Thieving/Stall.rsobj.cs`

## Implementation path

1. Keep NPC/stall target resolution in the corresponding scripts. Resolve loot, level and chance through definitions and award via existing item/container operations.
2. Stun/cooldown effects should use existing character states/tick tasks; avoid independent timers and per-NPC lock frameworks without demonstrated concurrency.

## Regression acceptance

- Stealing from an invalid/stunned target yields no loot; inventory full prevents loss; failure retaliation and success XP trigger exactly once; other players cannot bypass stall reset.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.

## Extended 2012 candidate datasets

- [22 pickpocket families](data/researched/thieving-pickpocket-targets.csv), [ten locked-door gates](data/researched/thieving-locked-doors.csv) and [the failure/loot/collision contract](reference/deep-thieving.md). Quest-modified loot, probabilities and NPC/object IDs remain open.
