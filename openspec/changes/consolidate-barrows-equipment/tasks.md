## 1. Characterization

- [x] 1.1 Add `BarrowsEquipmentTests` pinning 140 stage IDs, set detection, unequip and registration; pass against the unrefactored scripts.

## 2. Implementation

- [x] 2.1 Add `BarrowsEquipment` and migrate the seven scripts, removing local algorithms.
- [x] 2.2 Update registration-gap docs to distinguish separately registered weapon scripts.

## 3. Verification

- [x] 3.1 Run `Hagalaz.Game.Scripts.Tests` (460) and GameWorld tests (1457): all pass.
- [x] 3.2 `dotnet build Hagalaz.sln` (0 errors), `git diff --check`, `openspec validate` pass.
- [x] 3.3 jscpd (unchanged `.jscpd.json`, Barrows scripts): 21 clones before, 0 after.
