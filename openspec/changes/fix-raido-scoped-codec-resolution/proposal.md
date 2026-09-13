## Why

World entry reaches the first character-render update, but Raido resolves its
codec from the root provider. The `DrawCharactersMessageEncoder` requires the
scoped `ICharacterRenderMasksWriter`, so encoding fails and the client is
disconnected while it is still showing the world-loading screen.

## What changes

- Register each Raido codec with the same scoped lifetime as the protocol that
  owns its connection-specific encoders and decoders.
- Add regression coverage for resolving a scoped encoder through a protocol
  codec.

## Impact

This changes the Raido protocol codec lifetime at the connection/protocol
boundary. It preserves the existing encoder registry and message wire format.

## Acceptance criteria

- A protocol codec cannot be created from the root provider.
- A protocol resolved in an active scope can encode a message whose encoder has
  scoped dependencies.
- The GameWorld build and focused Raido regression test pass.
- The client can enter the world after lobby sign-in without the server
  disconnecting during character rendering.

## Non-goals

- Do not change the character render-mask implementation or packet format.
- Do not change world authentication, map loading, reconnect, or session
  cleanup behavior.
- Do not add another service scope or cache for encoders.
