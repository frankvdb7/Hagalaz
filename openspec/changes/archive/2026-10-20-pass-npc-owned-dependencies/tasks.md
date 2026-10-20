## 1. Compose the NPC graph from the child scope

- [x] 1.1 Change `NpcBuilder` to resolve the selected script, definition, and
  typed constructor dependencies from the child scope and call `Npc` directly
  with explicit runtime inputs.
- [x] 1.2 Pass the typed NPC service into `NpcHandle` without adding another
  scope owner.
- [x] 1.3 Move ordinary NPC services into required constructors and activate
  every NPC script with its owner.

## 2. Migrate NPC-owned components

- [x] 2.1 Give `Npc` explicit event, map, movement, script, combat, loot, and
  rendering dependencies while preserving initialization order.
- [x] 2.2 Pass direct services into `Movement`, `NpcAppearance`, and
  `NpcStatistics`.
- [x] 2.3 Pass direct combat, loot, path-finding, options, and hit-splat
  dependencies into `NpcCombat` and its shared combat base path.

## 3. Migrate related NPC script paths and callers

- [x] 3.1 Update familiar and specialized NPC scripts to receive required
  services directly, preserve familiar hydration/respawn through the existing
  familiar character lifecycle, and adapt active familiar summoning within the
  NPC composition boundary.
- [x] 3.2 Replace concrete NPC-script dialogue/widget provider calls with the
  typed widget activator helper and update affected callers.
- [x] 3.3 Update focused construction and familiar lifecycle tests, including
  movement, omitted optional NPC values, owner-aware familiar activation and
  restoration, and attachment cleanup.

## 4. Validate the change

- [x] 4.1 Run focused GameWorld and Game.Scripts tests, then build affected
  projects and the solution as feasible.
- [x] 4.2 Run strict OpenSpec validation, scan the covered NPC graph for
  ordinary provider lookups, and review the cumulative diff for scope and
  lifecycle regressions.
