# Tasks

## 1. Shared contact feature implementation

- [x] 1.1 Replace the duplicate feature classes with one concrete `ContactsFeature` composed with `ContactPresenceState`; update all production and test construction sites, then verify the focused contact-presence tests pass.
- [x] 1.2 Add a characterization test proving a character-backed `ContactsFeature` reads the character's current friend and ignore lists after construction, and verify it passes.

## 2. Refactor verification

- [x] 2.1 Run `npx jscpd --reporters ai --summary Hagalaz.Services.GameWorld` and `git diff --check`; verify the two feature implementations are no longer reported as a clone and the diff has no whitespace errors or unrelated files.
