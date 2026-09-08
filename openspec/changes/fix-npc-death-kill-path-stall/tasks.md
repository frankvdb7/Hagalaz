## 1. Remove synchronous waits from kill-path service work

- [x] 1.1 Queue Slayer kill accounting and make completion reward lookups asynchronous.

## 2. Regression coverage and validation

- [x] 2.1 Add Slayer regressions with pending task-definition and completion reward lookups and assert the game-loop boundary remains non-blocking.
- [x] 2.2 Run focused tests, validate the OpenSpec change strictly, inspect the diff, and restart the affected GameWorld resource for manual confirmation.
