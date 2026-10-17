# Thieving: pickpockets, locked doors and failure mechanics — revision 742

## Source-led candidate inventory

- [22 NPC target families](../data/researched/thieving-pickpocket-targets.csv) include required level, success XP, broad loot categories, stun duration, failure life-point damage and quest gates. Source: [period article](https://wiki.darkan.org/index.php?title=Thieving&oldid=31000). Numeric NPC definition IDs and loot weights are **not** present.
- [10 locked-door candidates](../data/researched/thieving-locked-doors.csv) cover level gates, XP and lockpick requirements. Object variants, opening direction and destinations are deliberately blank.
- The source defines potential multi-loot checks (not guaranteed reward): double loot requires Thieving ≥ base+10 and Agility ≥ base; triple requires base+20/base+10; quadruple requires base+30/base+20. Bonus loot **does not grant additional Thieving XP**. Certain high-tier NPC rows explicitly omit 4× loot.
- `Lost Her Marbles` can modify coin/seed loot for early targets. H.A.M. equipment may affect capture odds. Do not turn these secondary claims into universal multipliers without a captured behavior trace.
- The preserved article's simplistic ~1.011%/level success narrative is **not authoritative**: do not encode that as a linear success formula. Existing skill logic, gear bonuses, NPC class, true probability and target build require verification.

## Repository and authority

`Hagalaz.Game.Scripts/Skills/Thieving/StandardPickPocketTask.cs`, `StandardStealTask.cs`, `PickPockatableNPC.rsnpc.cs` and `Stall.rsobj.cs` already implement theft entry points. The NPC spawn/instance is the target identity; the character owns its stun state and XP; the established item container owns loot. If the server already has loot/drop tables, join by verified NPC definition and option rather than duplicating probabilities in each script.

## Click-to-result pipeline

1. Resolve the *live* NPC or door, target plane, reachable position and menu option; ensure target exists and is eligible rather than trusting a client-stated object ID.
2. Check unlock quest, minimum effective level, cooldown/stun state and relevant tool. For doors, check direction and collision change; for a safe, check trap policy and respawn state.
3. Roll once at the defined action point. On success, verify available inventory slots and perform a single loot selection and item mutation; add base XP **once**, regardless of any bonus quantity.
4. On failure, publish the stun/disable duration and LP damage exactly once; prevent a second attempt until stun expires. Avoid routing stun damage through ordinary protection-prayer melee mitigation unless target evidence requires it.
5. A target death, despawn, option change, combat state invalidation, movement cancel or logout must terminate the task without granting delayed loot.
6. Shared stalls and loot objects need a single world-owned depletion/respawn mechanism; repeated client clicks do not instantiate independent stock.

## Data still required for complete parity

Resolve each NPC variant to 742 index-18 definition and spawn map; derive loot outcomes/weights, actual success function, loot bonus roll probabilities, NPC-specific pickpocket option, exact scripts/animations, stun tick conversion and quest state. For each door/stall, resolve index-16 object + collision delta, restored scene object and per-world cooldown. The names in the CSV are human reference labels only.

## Acceptance cases

Exactly at/below/above level gate; inventory full; NPC death between click and completion; correct loot without bonus; 2×/3×/4× checks and no extra XP; immunity to duplicate completion callback; quest-gated H.A.M. guard; equipment changes before resolution; failed pickpocket under active protection prayer; concurrent loot of the same stall; door collision transitions under opposing movement; reconnect after stun and after committed loot.

[Implementation guide](../thieving.md) · [Cache evidence](client-cache-evidence.md).
