Use the existing `CharacterStateProfile` and `CharacterService.GetStateAsync` projection path. Align `StateDto.StateExDto.Id` and the Characters service `State.StateEx.Id` with the persisted `CharactersState.StateId` type (`string`). Keep the existing GameWorld hydrated state model, which already uses string identifiers.

Update the snapshot persistence profile and replacement lookup to compare the string identifier directly. This avoids parsing GUID-based identifiers and keeps the same value across database, MassTransit message, and GameWorld hydration.

The regression test will use the existing EF Core InMemory provider and the real AutoMapper profile, then execute `ProjectTo` against a `CharactersState` row. This tests the failing queryable boundary rather than only testing in-memory object mapping.
