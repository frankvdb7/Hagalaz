## 1. Replace former locator consumers

- [x] 1.1 Inject `IOptions<WorldOptions>` into `AreaScript` and update every concrete area-script constructor without changing `IAreaScript.Initialize(IArea)`.
- [x] 1.2 Inject `IProjectileBuilder` into `SpellBookTab`, convert the five ancient-spell helpers to use it directly, reuse `_magicService`, and remove the uncalled `LoadScriptedCombatSpells` method.

## 2. Remove the global locator implementation

- [x] 2.1 Remove GameWorld global provider setup, obsolete usings, and all live or commented locator references.
- [x] 2.2 Delete the locator project and remove its solution/project/package references, adding direct DI abstractions ownership where required by existing callers.

## 3. Add regression coverage

- [x] 3.1 Add an MSTest regression proving an injected `WorldOptions` value controls an area script's respawn location.
- [x] 3.2 Add a focused `SpellBookTab` DI activation/lifecycle test or equivalent compile-time composition coverage proving the explicit projectile-builder dependency does not create a circular or recursive initialization path.

## 4. Validate and close the change

- [x] 4.1 Run focused tests and the solution build, then validate the OpenSpec change with strict mode.
- [x] 4.2 Run a final source search confirming no locator references, replacement facade, new `LoadScriptedCombatSpells` call, or recursive spellbook initialization remains.
