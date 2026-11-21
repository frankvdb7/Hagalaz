## Why

The Barrows run lifecycle in `BarrowsScript` had blockers listed in issue #315: its setup ran before the injected builders were assigned, a normal exit erased unclaimed progress, the hidden tunnel was drawn from seven values although there are six mounds, the HUD packed the chest flag and kill count into the same bit, and the config 452 rope encoding disagreed with the 742 cache.

## What Changes

- Load the run from the `BarrowsScript` constructor body, after dependencies are assigned.
- Keep unclaimed progress on exit; leaving always sends the existing "power drains" message.
- Choose the hidden tunnel among the six mounds, persist it at creation, and pick a new one when the chest is searched.
- Searching the chest clears kills and records the claim; the claim flag stays until exit so a run cannot be claimed twice, and a claimed chest no longer spawns the tunnel brother.
- Derive the brother count (prayer drain) from killed flags; credit a kill only to the brother spawned for that player.
- Death keeps the run and despawns living brothers, leaving a brother that is already dead so its delayed kill credit still fires.
- Make Akrisae a melee attacker with a small protection state (switch after 150 damage from one style, skip an attack); no general NPC prayer system.
- Ahrim's successful magic hits lower the target's Strength by 5 levels with a 20% chance.
- Keep Akrisae locked until a persisted quest-completion source exists.
- Roll `min(N+1, 7)` chest rewards: each is a slain brother's armour table or an ordinary band chosen by reward potential (crypt-creature combat levels capped at 1,000, plus 2 per brother); chest loot table 8 is no longer rolled and the tables are seeded by a C# seeder that creates a table only when its id is absent (no SQL, no migration, never overwrites).
- Add the centre-door puzzle: one operable chest-room door per run, interface 25 with the four shape puzzles, locked doors via config 452 bits 10-25, a wrong answer moves the operable door and the exit rope.
- Pack config 453 as brother flags (bits 0-5), chest open (bit 16), kill count (bits 17-31); send config 452 as `1 << (6 + entry)`; register rope 6712.

### Non-goals

- Success and failure chat messages for the puzzle, re-randomizing every door on a wrong answer, and perimeter door locking (no source states them).
- Verified 742 rates (reward and armour rolls use the published reward-potential model), the ring of wealth bonus drop, and the later Linza, Rise of the Six and defender drops.
- Quest completion persistence and the separate Akrisae crypt.
- Barrows equipment (`barrows-equipment`).

## Capabilities

### New Capabilities

- `barrows-run-lifecycle`: per-player Barrows run state, claim, reset and HUD/config encodings.

## Impact

- `Hagalaz.Game.Scripts/Minigames/Barrows/*`
- `Hagalaz.Game.Scripts.Tests/Minigames/Barrows/BarrowsRunLifecycleTests.cs`
- `docs/revision-742/reference/minigames-barrows.md`
