# Proposal

## Why

`LobbyContactsFeature` and `WorldContactsFeature` repeat the same `IContactsFeature` forwarding implementation while already sharing `ContactPresenceState`. Keeping both copies makes contact-presence operations easier to change inconsistently. Consolidating the shared behavior should preserve the existing lobby-owned and character-owned contact lists.

## What Changes

- Replace the duplicate `LobbyContactsFeature` and `WorldContactsFeature` implementations with one concrete `ContactsFeature` that composes the existing `ContactPresenceState`.
- Give `ContactsFeature` a lobby constructor that owns new contact lists and a world constructor that reads the character's current lists.
- Update the in-repository production and test construction sites to use `ContactsFeature`.
- Keep the existing `IContactsFeature` contract and all session-generation, observation-window, and notification behavior unchanged.

## Scope and Acceptance Criteria

- Only the duplicate contact-feature forwarding implementation and its focused tests may change.
- Production callers construct one `ContactsFeature` for each connection, using the appropriate lobby-owned or character-backed lists.
- Both features expose the same presence operations and preserve list identity and ownership.
- Character-backed `Friends` and `Ignores` continue to reflect the character's current lists when those properties are replaced.
- Existing contact-presence tests pass, and the GameWorld jscpd scan no longer reports the feature implementation clone.
- If the extraction requires changing observable contact behavior, list ownership, or the `IContactsFeature` contract, stop and propose a separate change.

## Non-Goals

- No changes to contact session admission, generation fencing, contact notifications, snapshot loading, or sign-in sequencing.
- No static helper, inheritance base, dependency, registration, or second contact-presence state owner.
- No priority-queue or other collections refactoring in this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an implementation-only refactor; the existing `contact-session-presence` behavior remains the contract.

## Impact

- `Hagalaz.Services.GameWorld/Features/ContactsFeature.cs`
- The existing lobby and world feature construction sites
- Existing GameWorld tests that exercise the contact feature boundary

No external dependencies or runtime registrations are required.

The old concrete type names are removed from this service implementation; direct consumers should construct `ContactsFeature` instead. The `IContactsFeature` runtime contract is unchanged.
