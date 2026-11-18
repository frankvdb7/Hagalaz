# Spec Delta

## Purpose

Lets operators track death and skill activity rates through bounded GameWorld counters without recording player identity or progression.

## ADDED Requirements

### Requirement: Count completed combat death transitions

GameWorld MUST add one to `hagalaz.gameworld.combat.death` when an actor transitions from alive to dead. The sole measurement tag MUST be `actor.kind`, with `player` or `npc` identifying the actor that died.

#### Scenario: An actor takes damage

- **GIVEN** a living player or NPC
- **WHEN** a hit misses or leaves the actor alive
- **THEN** no death is counted
- **WHEN** a later hit transitions the actor to dead
- **THEN** exactly one death is counted for the actor's kind

#### Scenario: The same death is observed repeatedly

- **GIVEN** a death has been counted
- **WHEN** death handling is invoked again while the actor remains dead or downstream kill notifications are delivered again
- **THEN** the count stays the same

#### Scenario: The actor dies after respawn

- **GIVEN** the actor has respawned after a counted death
- **WHEN** the actor transitions to dead again
- **THEN** one new death is counted

### Requirement: Count resolved combat kill credit

GameWorld MUST add one to `hagalaz.gameworld.combat.kill.credited` when the original death's deferred combat handler resolves a player or NPC as the credited killer. The sole tags MUST be `killer.kind` and `victim.kind`, each limited to `player` or `npc`.

#### Scenario: A death has a credited killer

- **WHEN** GameWorld resolves a killer for a counted death
- **THEN** exactly one credited kill is counted for the resolved killer and victim kinds
- **AND** repeated death handling and kill notifications add no extra counts

#### Scenario: A death has no resolved killer

- **WHEN** GameWorld processes a death without a resolved killer
- **THEN** the death is counted and there is no credited kill

### Requirement: Count successful skill actions

GameWorld MUST add one to `hagalaz.gameworld.skill.action.completed` each time Cooking replaces a raw food item with cooked food or a Fishing attempt adds at least one catch to inventory. The sole measurement tag MUST be `skill`, limited to `cooking` or `fishing`.

#### Scenario: A batch cooks multiple items

- **GIVEN** a player starts a Cooking batch
- **WHEN** two separate actions each replace a raw food item with cooked food
- **THEN** two completions are counted, one for each replacement

#### Scenario: Cooking produces no cooked food

- **WHEN** Cooking is interrupted before production, the raw item or cooking object is missing, or the food burns
- **THEN** there is no successful Cooking completion for that attempt

#### Scenario: A player reconnects

- **GIVEN** the player's Cooking action or death has already been counted
- **WHEN** the player resumes the existing world session
- **THEN** reconnect records no additional completion for that action or death

#### Scenario: Fishing adds multiple catches during a single attempt

- **WHEN** a single fishing attempt adds one or more catches to inventory
- **THEN** one Fishing completion is counted

#### Scenario: Fishing produces no inventory reward

- **WHEN** fishing is interrupted before the catch, the roll yields nothing, or inventory accepts none of the catches
- **THEN** no Fishing completion is counted
