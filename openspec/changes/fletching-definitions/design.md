# Design

## Decisions

- **Reuse the entity.** Primary key stays `resource_id`; it is unique across all 78 current recipes. A `category` byte is the only addition required: category selects the callback, tick delay and script factory and cannot be inferred from tool ids. `animation_id` becomes nullable because crossbow (u) recipes have none (code used -1).
- **In-memory read model.** `FletchingService` (`IStartupService`) loads entities, maps them in memory (comma lists cannot be translated by `ProjectTo`), and serves synchronous lookups, so the game loop never waits on I/O and no `.Result` is used. It is registered before `ItemScriptProvider`, because the item script factories enumerate recipe resource ids when the provider loads (startup services run sequentially in registration order).
- **Culture.** Lists are written and parsed with `CultureInfo.InvariantCulture`.
- **Lookup.** `FindRecipe(category, usedId, usedWithId)` matches (resource, tool) in either orientation within the category.
- **Seeding.** C# seeder, schema-only migration. Absent rows are inserted; an existing row is only rewritten while its `category` is 0 (a legacy row never claimed by this seeder), so later operator edits are preserved.
- **Tick delay** (wood 3, bow 2, ammo 1, tips 3) stays in script code with the callback that uses it.

## Risks

- Edits need a restart (same as other preloaded stores).
- Source values that look wrong (emerald bolts output 9335, sapphire bolts 9337) are seeded verbatim.
