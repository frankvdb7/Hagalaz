# Friends, private messages and friends chat — revision 742

**Repository assessment:** Messaging contracts and contact service present. Code entry points do not imply full behavior coverage.

## Historical scope

Friends/ignore relationships, presence, privacy, private messages, friends-chat channel membership, ranks, kicks and cross-world propagation.

## Existing Hagalaz entry points

- `Hagalaz.Services.Contacts/`
- `Hagalaz.Contacts.Messages/GetContactsRequest.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/FriendsChatTab.rsi.cs`
- `Hagalaz.Game.Messages/Model/NotifyFriendsChatMessage.cs`

## How to implement

1. Use Contacts as authority for relationship/privacy data and current message consumers for world visibility. Use a channel's existing owner for membership/rank enforcement; avoid a duplicate online-members cache.
2. Map 742 privacy/config and chat packet semantics in protocol decoders/encoders; ensure messages from blocked/hidden senders respect the target's settings.

## Verification criteria

- Cross-world friend visibility converges; ranks and kicks are authorized server-side; reconnect does not duplicate subscriptions or chat history; private status is not leaked.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- Historical behavior and resource IDs require direct 742 fixture verification.
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
