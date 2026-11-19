## Why

The Barrows run lifecycle in `BarrowsScript` had blockers listed in issue #315: its setup ran before the injected builders were assigned, a normal exit erased unclaimed progress, the hidden tunnel was drawn from seven values although there are six mounds, the HUD packed the chest flag and kill count into the same bit, and the config 452 rope encoding disagreed with the 742 cache.

## What Changes

- Load the run from the `BarrowsScript` constructor body, after dependencies are assigned.
- Keep unclaimed progress on exit; leaving always sends the existing "power drains" message.
- Choose the hidden tunnel among the six mounds, persist it at creation, and pick a new one when the chest is searched.
- Searching the chest clears kills and records the claim; the claim flag stays until exit so a run cannot be claimed twice, and a claimed chest no longer spawns the tunnel brother.
- Derive the brother count (prayer drain) from killed flags; credit a kill only to the brother spawned for that player.
- Death keeps the run and despawns living brothers, leaving a brother that is already dead so its delayed kill credit still fires.
- Keep Akrisae locked until a persisted quest-completion source exists.
- Roll chest armour only from slain brothers' tables (armour moved out of loot table 8 by a manual SQL script, no migration).
- Add the centre-door puzzle: one operable chest-room door per run, interface 25 with the four shape puzzles, locked doors via config 452 bits 10-25, a wrong answer moves the operable door.
- Pack config 453 as brother flags (bits 0-5), chest open (bit 16), kill count (bits 17-31); send config 452 as `1 << (6 + entry)`; register rope 6712.

### Non-goals

- Success and failure chat messages for the puzzle, entry-room rerolls on a wrong answer, and perimeter door locking (no source states them).
- Ordinary reward scaling from crypt-creature kills and verified 742 rates (armour rolls use later-era defaults).
- Brother specials (Akrisae protection switching, Ahrim Strength drain) and the 10s-versus-18s prayer-drain interval.
- Quest completion persistence and the separate Akrisae crypt.
- Barrows equipment (`barrows-equipment`).

## Capabilities

### New Capabilities

- `barrows-run-lifecycle`: per-player Barrows run state, claim, reset and HUD/config encodings.

## Impact

- `Hagalaz.Game.Scripts/Minigames/Barrows/*`
- `Hagalaz.Game.Scripts.Tests/Minigames/Barrows/BarrowsRunLifecycleTests.cs`
- `docs/revision-742/reference/minigames.md`
