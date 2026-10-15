# Agility: ordered route evidence and region collision — revision 742

## Concrete data

- [13 course candidates](../data/researched/agility-course-candidates.csv) list access levels, conditional rewards, quests and session rules.
- [38 ordered obstacles/bonuses](../data/researched/agility-obstacle-sequences.csv) for basic/advanced Gnome, advanced Barbarian, Wilderness and Ape Atoll. Rows separate per-obstacle XP from the completion-only bonus; object IDs and exact movements remain **unknown**.
- [31 shortcut candidates](../data/researched/agility-shortcut-candidates.csv) with level, area, XP and prerequisites. Their positions and 742 object IDs are intentionally blank.
- Sources: [historical course overview](https://wiki.darkan.org/Agility) and [Gnome-specific description](https://wiki.darkan.org/Gnome_Agility_course). The source sometimes groups completion rewards with the final obstacle; when this differs, the full-lap total is the candidate, not a guessed per-event packet breakdown.

## Lap state and world ownership

Existing `Hagalaz.Game.Scripts/Skills/Agility/Agility.cs` and course-specific object scripts, including `Courses/Gnome/LogBalance.rsobj.cs` and `Signpost.rsobj.cs`, are the preferred extension seams. Do not create a universal course engine merely to implement a second route. One active character action may own its current obstacle interaction; a **minimal per-course lap position** may live in character state if the server cannot otherwise detect completed sequence.

1. Only a live accessible obstacle clicked in reach can begin. Server verifies current tile/plane, collision, skill gate, quest/appearance requirement and the specific object option.
2. Transition on the movement task's *actual completion*, not click receipt; movement can fail due to a disappearing object, region unload, changed path, teleport, logout or death.
3. Reward obstacle XP once on success, update route stage **only after** the legitimate stage finishes, and award the completion bonus only if the expected prior stages were visited in order. A skipped stage cannot be made up by repeated final clicks.
4. The source records differing failure semantics: basic Gnome has no failures; advanced Gnome signpost can fail without a perfect lap; Wilderness course sits in a dangerous PvP region; shortcuts can damage or redirect to another plane.
5. Map/object destination and map-scene transform changes are owned by the existing world region/object and character movement services. Do not disable collision globally to make one shortcut work.
6. On logout/teleport/death, end an in-progress course action and invalidate transient partial-lap progress unless a **verified** historical checkpoint persistence rule requires otherwise. Completing a rewarded lap must remain idempotent.

## Historical conflicts and special cases

- The Gnome-specific source expresses the final pipe XP as the combined final obstacle + completion; overview instead splits 7.5 and 39, yet both report a **86.5** lap. Compare live XP events before selecting one attribution.
- The advanced Gnome report gives a 630 last-obstacle display including **605** completion bonus; do not award 630 plus 605.
- The Barbarian basic course overview's printed per-obstacle sum differs from its printed total **139.5**. The per-step table is excluded from this machine-readable pack pending reconciliation.
- The Werewolf course has a **190-XP retrieval bonus**; Dorgesh-Kaan varies reward by outbound/return route; Werewolf Skullball varies XP by completion time. These are not ordinary constant-XP course laps.
- Advanced Gnome/Barbarian perfect-lap counters (250) have failure-specific progression; character reward unlock is distinct from transient lap position.

## Acceptance fixtures

Each of the five ordered route datasets should have one successful complete lap with the precise sum; duplicate last obstacle, skipped stage, premature last-stage click, failed signpost, death mid-obstacle, disconnect, movement collision, two simultaneous obstacles, boosted entry level expiring after entry and reward count 249→250 must have testable outcomes. For shortcuts test source/destination tile, two-way restriction, safe blocked fallback, level below requirement and quest gate.

## Still missing

Verified cache index-16 object identities, map coordinates and shape/rotation, animation/graphics IDs, path targets, failure probability/LP formula, exact tick durations, UI/tick XP emission and scene clipping traces for the 742 client. No candidate CSV may be loaded directly as world scene config.

[Implementation guide](../agility.md) · [Cache evidence](client-cache-evidence.md).
