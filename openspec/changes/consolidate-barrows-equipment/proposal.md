## Why

The seven Barrows equipment scripts (`Ahrim`, `Akrisae`, `Dharok`, `Guthan`, `Karil`, `Torag`, `Verac`) each carry a private copy of the same set-detection and degradation rules (issue #534, jscpd report from #533). The copies can silently diverge. No gameplay change is intended.

## What Changes

- Add one abstract `BarrowsEquipment : DegradingEquipment` that owns complete-set detection, degradation ticks, the on-death factor and the degraded-item mapping.
- Each concrete script keeps its own `EquipmentScriptMetaData`, ordered 20-ID table and typed effect state, and only decides which state to add or remove.
- Characterization tests pin all 140 stage IDs, set detection, unequip and the Guthan/Karil registration split before and after the refactor.

### Non-goals

- No change to `DegradingEquipment`, `EquipmentScriptMetaDataFactory`, equipment containers, persistence or the state framework.
- No change to item IDs, timings or balance, and no change to `GuthansWarSpear`/`KarilsCrossbow` registrations (their weapon IDs must not be added to the parent attributes).
- `DegradeTask` countdown lifecycle (#521), Barrows minigame content (#315) and jscpd configuration (#507) are out of scope.

## Capabilities

### New Capabilities

- `barrows-equipment`: Barrows set detection and degradation behavior.

## Impact

- `Hagalaz.Game.Scripts/Equipment/Barrows/*`
- `Hagalaz.Game.Scripts.Tests/Equipment/BarrowsEquipmentTests.cs`
- `docs/revision-742/` registration-gap wording.
