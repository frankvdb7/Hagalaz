# Design

## Context

See proposal.md - Why. Both existing feature classes delegate to one `ContactPresenceState`, but the state and forwarding operations are repeated. Lobby owns its `ContactList` instances; world reads the lists from its `ICharacter`.

## Goals / Non-Goals

**Goals:** Put the shared `IContactsFeature` implementation and its single `ContactPresenceState` owner in one concrete instance that uses composition. Preserve lobby-owned lists and dynamic character-backed list access.

**Non-Goals:** Change presence rules, alter the interface, or consolidate other contact or collection implementations.

## Decisions

### Use one composed feature instance

Add one concrete `ContactsFeature` that implements `IContactsFeature` and owns one `ContactPresenceState`. Its parameterless constructor creates the lobby friend and ignore lists; its `ICharacter` constructor reads the character's current lists. Store list accessors as instance delegates so the world form remains dynamic rather than capturing the lists once.

Replace the two duplicate feature classes and update their production and test callers to construct `ContactsFeature` directly. Each connection still receives its own feature instance and presence state. Friend and ignore list ownership remains with the lobby feature instance or the character, according to the constructor used.

The old concrete class names are service implementation details with no in-repository production consumers beyond the construction sites being updated. `ContactsFeature` remains public and implements the unchanged `IContactsFeature` contract.

Alternatives considered:

- An abstract base class would remove the clone but use inheritance for two concrete feature variants; retain a concrete implementation composed with the existing presence state.
- Keeping wrapper classes around a composed object would leave the same interface-forwarding methods duplicated.
- Capturing the character's lists in the constructor would change current behavior if the character replaces either list; use instance accessors instead.

## Risks / Trade-offs

- Direct consumers of the removed service implementation type names must migrate to `ContactsFeature` → update all in-repository callers and verify the service project and focused tests compile.
- Moving the methods could accidentally change which list instance an operation mutates → keep character accessors dynamic and lobby accessors bound to the newly owned lists; test identity after character-list replacement.

## Migration Plan

No data migration is required. Replace the two feature implementations with the composed concrete class, update callers, then run focused GameWorld contact-presence tests and the same GameWorld jscpd scan. Rollback is a revert of the consolidated feature and caller updates.
