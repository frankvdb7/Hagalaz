## ADDED Requirements

### Requirement: Barrows set effect requires a complete set

A Barrows set effect state SHALL be added on equip only when the hat, weapon, chest and legs slots all hold pieces of the same set, at any degradation stage, and SHALL be removed when any piece of that set is unequipped.

#### Scenario: Complete set at mixed stages

- **WHEN** four pieces of one set at different degradation stages are equipped
- **THEN** exactly that set's effect state is added

#### Scenario: Incomplete or mixed set

- **WHEN** a slot is empty, holds a piece of another set, or holds an unrelated item
- **THEN** no effect state is added

#### Scenario: Unequip

- **WHEN** any piece of the set is unequipped, including the Guthan war spear or Karil crossbow
- **THEN** that set's effect state is removed

### Requirement: Barrows degradation rules are unchanged

An original Barrows piece SHALL have 0 degradation ticks and degrade to its declared first degraded ID; every later stage SHALL have 22,500 ticks and degrade to `id + 1`; the on-death factor SHALL be 0.80.

#### Scenario: Stage table

- **WHEN** degradation is queried for any of the 140 declared set IDs
- **THEN** ticks, factor and successor match the characterized table

### Requirement: One handler per Barrows item ID

Each declared Barrows item ID SHALL resolve to exactly one equipment script; the Guthan war spear and Karil crossbow IDs SHALL resolve to their weapon scripts, not the armour scripts.

#### Scenario: Registration split

- **WHEN** equipment scripts are discovered
- **THEN** each of the 140 IDs has one handler, and the 10 weapon-script IDs resolve to `GuthansWarSpear`/`KarilsCrossbow`
