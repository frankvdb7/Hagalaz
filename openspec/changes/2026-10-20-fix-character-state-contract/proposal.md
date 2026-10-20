## Why

The real client reaches the lobby, but pressing Play Game fails during character hydration. The Characters service cannot project `CharactersState.StateId` into the response because persistence now stores state identifiers as strings while the response contract still declares the identifier as an integer.

## What changes

- Represent persistent character state identifiers as strings throughout the Characters service message and service models.
- Keep the existing projection and persistence paths string-to-string so GUID-based state metadata can be hydrated.
- Add a regression test that executes the EF/AutoMapper state projection with a string state identifier.
- Update persistence test fixtures to use the string contract.

## Impact

This affects character hydration and snapshot persistence for state identifiers. It does not change reconnect transport behavior, authentication, issuer configuration, state ownership, or the stored state identifier values.

## Acceptance criteria

- A character state row with a string/GUID identifier can be projected by the Characters service without an AutoMapper queryable-mapping exception.
- The state identifier is preserved in the outbound message and can be consumed by GameWorld state hydration.
- Existing Characters service tests pass with the corrected contract.

## Non-goals

- Do not change state metadata values or migrate existing state rows.
- Do not change GameWorld reconnect behavior or authentication.
- Do not add a fallback parser for integer-only state identifiers.
