# Auras and character customization — revision 742

**Repository assessment:** Aura item scripts and character design interfaces exist. Code entry points do not imply full behavior coverage.

## Historical scope

Cosmetic/functional auras, activation/cooldowns, character appearance, name/design interfaces and visual configuration available before the 742 cutoff.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Items/Auras/AuraItemScript.cs`
- `Hagalaz.Game.Scripts/Items/Auras/HarmonyAura.rsitem.cs`
- `Hagalaz.Game.Scripts/Widgets/CharacterDesign/DesignInterface.rsi.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/AppearanceDehydrator.cs`

## How to implement

1. Keep visual appearance and aura active-state persistence in existing character pipelines. Tie aura requirements and cooldowns to their item/definition scripts; do not share a generic shop state with appearance.
2. Validate 742 model/equipment appearance IDs from cache before displaying; distinguish cosmetic overrides from real equipment bonuses.

## Verification criteria

- Auras cannot stack illegally or reset cooldown on relog; design changes do not remove worn stats; client receives one consistent appearance state per change.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Update%3ALoyalty_Additions_and_Lodestone_Network)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
