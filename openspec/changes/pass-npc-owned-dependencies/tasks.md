## 1. Compose the NPC graph from the child scope

- [x] 1.1 Change `NpcBuilder` to resolve the selected script and definition from the child scope and activate `Npc` with typed dependencies and explicit runtime inputs.
- [x] 1.2 Pass the typed NPC service into `NpcHandle` and update unregister construction without adding another scope owner.
- [x] 1.3 Move the NPC owner and common script services into required base/script constructors and remove the owner-binding initialization API.

## 2. Migrate NPC-owned components

- [x] 2.1 Give `Npc` explicit event, map, movement, script, combat, loot, and rendering dependencies while preserving initialization order.
- [x] 2.2 Pass direct services into `Movement`, `NpcAppearance`, and `NpcStatistics`, and remove their owner-provider lookups.
- [x] 2.3 Pass direct combat, loot, path-finding, options, and hit-splat dependencies into `NpcCombat` and its shared combat base path.

## 3. Migrate related NPC script paths and callers

- [x] 3.1 Update familiar, player-NPC, and specialized NPC scripts to receive their required services directly, and defer familiar restoration until the owner-aware NPC composition step.
- [x] 3.2 Replace concrete NPC-script dialogue/widget provider calls with the typed widget activator helper and update all affected callers.
- [x] 3.3 Update focused construction tests (including movement and unregister) and verify existing script, spawn, and lifecycle suites remain green.

## 4. Validate the change

- [x] 4.1 Run focused GameWorld and Game.Scripts tests, then build the affected projects and solution as feasible.
- [x] 4.2 Run strict OpenSpec validation, scan the covered NPC graph for ordinary provider lookups, and review the final diff for scope, lifetime, and behavior regressions.
