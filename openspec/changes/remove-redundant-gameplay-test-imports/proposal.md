# Proposal

## Why

The last solution-wide Rider inspection reported 610 distinct redundant imports across the two gameplay test projects. Removing them makes the imports reflect what each file needs and clears inspection noise from active regression suites.

## What Changes

- Remove at least 500 Rider-confirmed redundant C# using directives in the Game Scripts and GameWorld test projects.
- Add and run focused gameplay characterization tests in both projects before removing imports.
- Preserve test behavior, symbol resolution, existing global imports, and production code. Limit the cleanup to confirmed redundant directives and stop when the scoped batch is verified.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

The change affects Hagalaz.Game.Scripts.Tests and Hagalaz.Services.GameWorld.Tests. It reuses the existing test fixtures, solution build, and Rider inspections.
