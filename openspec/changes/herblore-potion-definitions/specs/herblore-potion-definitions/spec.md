# Spec Delta

## Purpose

Defines how Herblore potion recipes, including overload, are persisted and resolved so scripts do not hard-code them.

## ADDED Requirements

### Requirement: Potion recipes are persisted
The system SHALL persist every Herblore potion recipe (product, kind, base/unfinished item, required level, experience, primary and secondary ingredients, secondary count) and expose it to scripts through synchronous `IHerbloreService` lookups.

#### Scenario: Unfinished potion from a primary ingredient
- **WHEN** a script looks up a seeded primary ingredient
- **THEN** the first standard recipe (by product id) using it is returned

#### Scenario: Finished potion from a secondary ingredient
- **WHEN** a script looks up a (base item, secondary ingredient) pair of a kind
- **THEN** the recipe of that kind is returned, and no recipe of another kind

#### Scenario: Shared unfinished potion
- **WHEN** several standard recipes share an unfinished potion and a script looks up that unfinished potion with one recipe's secondary ingredient
- **THEN** exactly that recipe is returned

#### Scenario: Final potion as base or ingredient
- **WHEN** a script looks up a final potion as the base item of a standard recipe, or any final potion consumed by the overload recipe
- **THEN** the recipe made from it is returned

#### Scenario: Extreme ranging needs five spikes
- **WHEN** an extreme ranging potion is made
- **THEN** five grenwall spikes are consumed, or nothing changes when fewer are present

### Requirement: Overload is a recipe
The system SHALL resolve overload from its definition (level, the five extreme potions, torstol, product, experience) and consume every extreme potion per overload.

#### Scenario: Torstol used on an extreme potion
- **WHEN** torstol is used on an extreme potion that is a secondary ingredient of the overload recipe
- **THEN** the overload flow starts with that definition

### Requirement: Existing recipes are seeded
The system SHALL seed all current recipes verbatim, inserting absent rows and never changing a row already seeded.

#### Scenario: Idempotent seeding
- **WHEN** the seeder runs twice, or after an operator changed a value
- **THEN** no duplicate rows exist and the changed value is preserved
