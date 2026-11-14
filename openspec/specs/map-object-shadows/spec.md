# map-object-shadows Specification

## Purpose
Defines how the admin map editor darkens floors under objects, following the revision 742 client.

## Requirements

### Requirement: Objects cast floor shadows

The map editor SHALL darken floors around walls, wall corners and ground objects whose definition casts a shadow.

#### Scenario: A wall casts a shadow

- **WHEN** a region contains a straight wall that casts a shadow
- **THEN** the two floor corners along that wall receive strength 50

#### Scenario: A ground object casts a shadow

- **WHEN** a region contains a static ground object that casts a shadow
- **THEN** every floor corner of its footprint receives its radius over 4, capped at 30

#### Scenario: A definition disables its shadow

- **WHEN** an object's definition does not cast a shadow
- **THEN** the floor around it is unchanged

#### Scenario: Overlapping shadows

- **WHEN** two objects write different strengths to one corner
- **THEN** that corner keeps the larger strength

### Requirement: Shadows follow the visible scene

The map editor SHALL derive shadows from the objects currently shown, including unsaved edits.

#### Scenario: An object is moved

- **WHEN** a draft moves an object
- **THEN** its shadow appears at the new position and not the old one

#### Scenario: A group is hidden

- **WHEN** the user hides an object or scene group
- **THEN** its shadow is removed

### Requirement: Shadows can be switched off

The map editor SHALL offer a view option that shows or hides object shadows without changing the cache or the draft.

#### Scenario: Turning shadows off

- **WHEN** the user turns off object shadows in the view layers
- **THEN** floors draw without object shadows
- **AND** turning the option on restores them

### Requirement: Loading does not churn the terrain

The map editor MUST NOT rebuild terrain for shadow changes while scene assets are loading.

#### Scenario: Assets are loading

- **WHEN** more appearances arrive during a load
- **THEN** the terrain keeps the previously settled shadows until the load ends
