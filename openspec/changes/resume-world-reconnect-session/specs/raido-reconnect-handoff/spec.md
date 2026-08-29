## Purpose

Allows a retained logical connection to accept one replacement physical transport without creating a second logical reader or losing unread input.

## ADDED Requirements

### Requirement: Replacement transport ownership is transferred atomically

The system MUST allow a retained logical connection to adopt one eligible replacement physical transport within its existing reconnect window. The handoff MUST transfer only physical transport ownership and preserve the consumed and unread input boundary. The retained logical connection remains the owner of protocol/cipher state, logical lifecycle, and completion state.

#### Scenario: Eligible replacement is adopted

- **GIVEN** a logical connection is retained after a transient physical disconnect
- **AND** a replacement physical connection has completed the handshake required to request adoption
- **WHEN** the replacement is committed
- **THEN** the old logical connection owns the replacement physical transport
- **AND** the old logical reader resumes at the consumed and unread input boundary
- **AND** no second logical reader processes the replacement transport

#### Scenario: Replacement loses the race

- **GIVEN** one replacement has already been reserved or committed for a retained logical connection
- **WHEN** another replacement attempts adoption
- **THEN** the second replacement is rejected
- **AND** the existing logical connection and first replacement remain the sole owners

### Requirement: A transferred temporary context cannot clean up the adopted transport

After a successful handoff, the temporary physical context MUST relinquish ownership without aborting or disposing the adopted transport. It MUST not invoke the normal logical disconnect callback.

#### Scenario: Successful handoff removes the temporary context

- **GIVEN** a replacement physical connection has been committed to the old logical connection
- **WHEN** temporary connection handling completes
- **THEN** only the old logical context remains in the connection store
- **AND** the temporary context is unregistered
- **AND** normal disconnect cleanup does not run for the temporary context
- **AND** the adopted transport remains usable by the old logical context

#### Scenario: Handoff is rejected before adoption

- **GIVEN** a replacement cannot be authenticated, validated, or committed
- **WHEN** temporary connection handling completes
- **THEN** the replacement is cleaned up by its own context
- **AND** the old logical reconnect lifecycle remains governed by the existing reconnect mechanism
