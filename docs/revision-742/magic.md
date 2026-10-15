# Magic spellbooks and casting — revision 742

**Repository assessment:** Partial; existing model and spell scripts. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Standard and alternate spellbooks, rune consumption, staff-provided runes, autocast, combat and noncombat spells, spell level checks, and teleports in the earlier combat model.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Magic.cs`
- `Hagalaz.Game.Scripts/Skills/Combat/Magic/StandardCombatSpell.cs`
- `Hagalaz.Game.Scripts/Skills/Magic/MiscSpells/AlchemyOperations.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/SpellBookTab.rsi.cs`

## Build approach

1. Use the existing magic state for rune availability, autocast and selected spell. Keep the specific spell effect in its spell script and the spell's definition/requirements in data. Validate cast target, range, rune cost, current skill and inventory before irreversible effects.
2. Match widget clicks/configurations and effect timing against the revision 742 client/cache; avoid assuming later spell availability or combat changes.

## Acceptance and tests

- Failed casts consume no runes or items; successful casts deduct exactly once; gear-provided runes apply correctly; autocast is cleared/revalidated when equipment or spellbook changes; teleports respect movement restrictions.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- Exact historical values and component mappings remain fixture-gated; do not replace unknowns with later-client defaults.
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.
