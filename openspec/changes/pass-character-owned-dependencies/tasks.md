## 1. Record and compose the character graph

- [x] 1.1 Add the explicit-character-dependencies delta spec and design decisions for scope ownership, required constructors, and narrow runtime activators.
- [x] 1.2 Add type-specific character, familiar, and character-NPC activator contracts and scoped implementations, then register them in GameWorld.
- [x] 1.3 Change `CharacterFactory` to activate `Character` from its nested scope with `ActivatorUtilities`, preserving scope disposal by `Creature`.

## 2. Migrate Character and owned components

- [x] 2.1 Inject `Character`'s direct service dependencies and update all Character partials to use those fields.
- [x] 2.2 Pass explicit dependencies into character-owned containers, appearance, render information, statistics, prayers, magic, music, slayer, widgets, and familiar item containers.
- [x] 2.3 Migrate familiar, character-NPC, dialogue, widget, and character-script runtime activation to their narrow typed boundaries without adding optional constructor parameters.
- [x] 2.4 Pass explicit dependencies into character-created movement/combat and farming patch tasks, while preserving existing non-character creature/NPC paths.

## 3. Update tests and callers

- [x] 3.1 Update focused component tests and Game.Scripts callers to pass direct dependencies instead of configuring owner service providers.
- [x] 3.2 Add regression coverage for nested character-scope composition, direct component construction, and runtime script activation.

## 4. Validate and close

- [x] 4.1 Run focused GameWorld and Game.Scripts tests, then build the solution with the repository's normal validation commands.
- [x] 4.2 Run strict OpenSpec validation and scan the covered character composition graph for ordinary provider lookups, optional constructor parameters added by this change, and missed call sites.
