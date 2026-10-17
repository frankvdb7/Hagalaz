# Tasks

## 1. Simplify transaction discovery

- [x] 1.1 Remove the forwarding transaction getter, update production and test callers to use `CurrentTransaction`, and simplify MoneyPouch's same-transaction check; verify discovery and MoneyPouch tests pass.

## 2. Wait for foreign mutation ownership

- [x] 2.1 Update standalone `BeginMutation` acquisition to wait under its monitor for foreign transaction or publication ownership, while preserving same-thread reentrancy and manually held-lock rejection; verify focused mutation concurrency tests pass.
- [x] 2.2 Add deterministic tests for waiting through transaction publication and standalone publication, including publisher failure and waiter release; verify rollback, publication counts, and boundary cleanup.

## 3. Integration validation

- [x] 3.1 Run Abstractions and relevant GameWorld tests, build the solution, run strict OpenSpec validation, and verify `git diff --check`.
