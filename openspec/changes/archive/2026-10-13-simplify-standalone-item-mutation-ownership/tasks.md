# Tasks

## 1. Boundary-owned standalone completion

- [x] 1.1 Extend the existing mutation scope to support nested ordinary mutations, explicit standalone completion, thread-affine disposal, and transaction participation; add focused tests for unlock-before-publication ownership, early disposal, no-op behavior, and provisional caller transactions, then run the Abstractions test project.

## 2. Domain mutation migration

- [x] 2.1 Replace single-participant transactions in Inventory, Bank, Familiar Inventory, and eligible Shop Stock operations with the boundary scope; verify existing domain behavior and publication tests in GameWorld.
- [x] 2.2 Update the canonical item-container storage requirement to state the standalone ownership and publication contract; verify the canonical and delta wording agree.

## 3. Transaction audit and validation

- [x] 3.1 Audit production transaction call sites and retain full transactions only where multi-container or multi-step rollback is required; verify each successful commit's owner publication path.
- [x] 3.2 Run the requested Abstractions, GameWorld, Scripts, Extensions, solution build, strict OpenSpec validation, C# duplication gate, and `git diff --check`; review the cumulative diff and confirm unrelated files remain unchanged.
