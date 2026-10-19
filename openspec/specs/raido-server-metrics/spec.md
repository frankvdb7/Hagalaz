# Raido Server Metrics Specification

## Purpose

Exposes bounded operational metrics for Raido application message dispatch, per-connection writes, stateful reconnect recovery, and client timeout termination through the existing Raido server meter.

## Requirements

### Requirement: Inbound dispatch metrics
The Raido server SHALL record one inbound dispatch count and duration for each received Raido message, tagged only with its bounded message type and one outcome: handled, unhandled, unauthorized, or error.

#### Scenario: Handled application message
- **WHEN** a received Raido message is dispatched and its handler completes
- **THEN** the server records one inbound dispatch with outcome `handled` and its dispatch duration

#### Scenario: Unhandled or unauthorized application message
- **WHEN** no registered hub method handles a received message, or authorization rejects its method
- **THEN** the server records one inbound dispatch with outcome `unhandled` or `unauthorized`, respectively

#### Scenario: Dispatch error
- **WHEN** dispatch processing fails while handling a received Raido message
- **THEN** the server records one inbound dispatch with outcome `error`

#### Scenario: Control message
- **WHEN** the server processes a Raido control message such as a ping
- **THEN** the server records one inbound dispatch with outcome `handled`

### Requirement: Outbound message write metrics
The Raido server SHALL record each per-connection application message write at the shared write boundary, tagged only with bounded message type and outcome, with duration measured through completion of the stable logical transport flush.

#### Scenario: Multi-client send
- **WHEN** one message is sent to multiple connected clients
- **THEN** the server records one write for each connection that reaches the Raido write boundary
- **AND** no recipient identity or payload data is recorded

#### Scenario: Write completion outcomes
- **WHEN** a write completes its stable logical transport flush, is canceled, fails, or cannot be admitted because the logical connection is detached or already terminal
- **THEN** the write is recorded with the corresponding bounded outcome `accepted`, `cancelled`, `failed`, `unavailable-detached`, or `unavailable` for an already-terminal connection
- **AND** `accepted` means the stable logical transport accepted the write and does not guarantee a physical transport write or client delivery

### Requirement: Stateful reconnect metrics
The Raido server SHALL record entry into the logical detached state and each reconnect completion outcome, with completion duration measured from detach until rebind or terminal completion.

#### Scenario: Successful reconnect
- **WHEN** a detached logical connection successfully rebinds to a physical connection
- **THEN** the server records one detach event and one reconnect completion with outcome `success` and the logical wait duration

#### Scenario: Reconnect timeout or terminal failure
- **WHEN** the detached logical connection times out or reaches another terminal state before rebind
- **THEN** the server records one completion with outcome `timeout` or `failure` and the logical wait duration

### Requirement: Client timeout metric
The Raido server SHALL expose client timeout termination through the Raido server meter and count only the transition that successfully terminates an active connection.

#### Scenario: Client timeout wins termination
- **WHEN** the client timeout check successfully transitions an active connection to terminal state
- **THEN** the server increments the client timeout metric exactly once

#### Scenario: Connection already inactive
- **WHEN** the timeout check observes an inactive connection or loses the terminal transition
- **THEN** the server does not increment the client timeout metric
