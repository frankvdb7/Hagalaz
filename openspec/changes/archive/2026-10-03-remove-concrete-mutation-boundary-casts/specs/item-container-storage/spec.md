# Spec Delta

## ADDED Requirements

### Requirement: Mutation boundaries expose an outside-transaction guard
A mutation boundary MUST provide a semantic operation that succeeds when its storage is outside an active transaction and throws `InvalidOperationException` when the storage is bound to an active transaction. The check MUST NOT mutate storage or change transaction lifecycle.

#### Scenario: Guard accepts storage outside a transaction
- **WHEN** the boundary's storage is not bound to an active transaction
- **THEN** the outside-transaction guard returns normally without changing storage

#### Scenario: Guard rejects transaction-bound storage
- **WHEN** the boundary's storage belongs to an active transaction
- **THEN** the guard throws `InvalidOperationException` and leaves storage and the active transaction unchanged
