## Purpose

Define the map-part update cutoff used by asynchronous producers and the
synchronous client update tick.

## ADDED Requirements

### Requirement: Client ticks use one frozen prepared update set

The system MUST allow producers to queue updates concurrently, freeze the
pending set once per client tick, send only that prepared set, and clear only
the prepared set when the tick completes.

#### Scenario: Updates are prepared for a tick

- **WHEN** `PrepareUpdatesForTick` is called
- **THEN** updates queued before the cutoff MUST be prepared and updates queued
  afterward MUST remain pending for the next tick

#### Scenario: Multiple characters receive the tick

- **WHEN** multiple characters are sent updates during one prepared tick
- **THEN** every character MUST receive the same frozen update set

#### Scenario: A producer queues during sending

- **WHEN** a producer queues an update while a character callback is running
- **THEN** the new update MUST not appear in the current prepared set and MUST
  survive completion for the next tick

### Requirement: Update queue operations are safe under concurrency

The system MUST deduplicate equal pending updates and MUST NOT hold its queue
lock while invoking character or network callbacks.

#### Scenario: Equal updates are queued concurrently

- **WHEN** equal updates are queued before preparation
- **THEN** only one instance MUST be sent for that tick

#### Scenario: A callback blocks

- **WHEN** sending invokes a callback that blocks
- **THEN** another producer MUST still be able to queue an update

### Requirement: Prepared generations have explicit completion ownership

A prepared update generation MUST remain owned until the client-update phase
explicitly completes or abandons it. A later preparation MUST NOT silently
replace an active prepared generation, and a failure in one region or
character MUST NOT skip completion for unrelated regions.

#### Scenario: A region update fails

- **WHEN** one region throws while delivering its prepared client updates
- **THEN** unrelated regions MUST still run their client-update phase
- **AND** every prepared region MUST reach an explicit completion or abandonment
  boundary before the next client tick begins

#### Scenario: One character update fails

- **WHEN** a character callback throws while another character is receiving the
  same client tick
- **THEN** the other character MUST still receive its prepared updates
- **AND** the failure MUST remain observable through the worker's error logging

#### Scenario: Preparation is attempted before completion

- **WHEN** a second preparation is requested while a prepared generation is
  still active
- **THEN** the operation MUST fail explicitly without discarding that
  generation
