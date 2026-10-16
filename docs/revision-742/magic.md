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

## Combat spell validation

[Ancient combat spell source arrays](data/catalogs/combat-ancient-spell-script-rules.csv) capture 20 explicit source variants across Blood, Ice, Miasmic, Shadow and Smoke. [P1 combat dossier](reference/deep-combat-character-mechanics.md#5-combat-magic-spells-autocast-and-rune-ownership) records source-only casting speed, rune debit, spell selection and the suspected double-applied magic maximum. The populated `skills_magic_combat_definitions` records and 742 client interface/packet mapping are not available here.

## P1 spellbook UI and CS2/varbit binding

The [UI/CS2 and varbit source crosswalk](reference/deep-client-interface-cs2-protocol-integration.md) maps index-3 spellbook components, index-12 script execution, index-22 varbits and normal vs global client state transport. [P19–P26 tests](reference/client-protocol-integration-verification.md) must prove spellbook layout, autocast/runecost UI events and server-authorized combat choices. Script names and signatures remain unverified until the pinned cache is decoded.
