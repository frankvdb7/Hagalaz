## 1. Synchronous handle cleanup with async compatibility

- [x] 1.1 Add synchronous NPC store and service operations while retaining `RegisterAsync` and `UnregisterAsync`.
- [x] 1.2 Route handle cleanup through synchronous unregistration, keep delayed death cleanup asynchronous, and use the lock's synchronous writer path for synchronous store mutation.
- [x] 1.3 Make `ICreature.OnRegistered` synchronous while retaining the asynchronous service and store APIs.

## 2. Regression coverage and validation

- [x] 2.1 Add deterministic GameWorld tests proving synchronous handle cleanup,
      retained async registration/unregistration, and synchronous store locking.
- [x] 2.2 Run the focused GameWorld tests, validate the OpenSpec change strictly, inspect the diff, and restart the affected GameWorld resource for manual confirmation.
