## Purpose

Keeps NPC death cleanup from stopping the single world update loop when asynchronous removal is delayed.

## ADDED Requirements

### Requirement: NPC death cleanup must not block world updates

The system MUST keep processing the world update loop while an NPC's asynchronous removal is pending after its death. The removal MUST still occur after the configured death-render delay through the existing NPC service.

#### Scenario: Permanent NPC removal is pending

- **WHEN** a non-respawning NPC reaches the end of its death-render delay and the asynchronous removal operation has not completed
- **THEN** the death task completes its current tick without blocking, and the removal operation remains scheduled for completion

#### Scenario: Permanent NPC removal completes

- **WHEN** the pending asynchronous removal operation completes
- **THEN** the NPC is removed through the existing NPC service and no synchronous wait is required on the world update thread

#### Scenario: Respawn fallback cannot spawn the NPC

- **WHEN** a respawning NPC reaches its respawn task and its script reports that it cannot spawn
- **THEN** removal is scheduled asynchronously and the world update thread remains available while removal is pending

#### Scenario: Respawning NPC dies

- **WHEN** an NPC configured to respawn reaches the end of its death-render delay
- **THEN** the existing respawn scheduling and behavior remain unchanged
