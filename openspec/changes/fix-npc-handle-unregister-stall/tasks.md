## 1. Synchronous handle cleanup with async compatibility

- [x] 1.1 Add synchronous NPC store and service operations while retaining `RegisterAsync` and `UnregisterAsync`.
- [x] 1.2 Route handle and delayed death cleanup through synchronous unregistration and use the lock's synchronous writer path.
- [x] 1.3 Make `ICreature.OnRegistered` synchronous while retaining the asynchronous service and store APIs.

## 2. Regression coverage and validation

- [x] 2.1 Add deterministic GameWorld tests proving synchronous handle cleanup and async registration remain available.
- [x] 2.2 Run the focused GameWorld tests, validate the OpenSpec change strictly, inspect the diff, and restart the affected GameWorld resource for manual confirmation.
