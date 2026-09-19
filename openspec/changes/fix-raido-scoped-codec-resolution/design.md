## Context

`DefaultRaidoCodecFactory` resolves registered encoder and decoder types from
the `IServiceProvider` captured when the codec is constructed. Protocols are
resolved in an owned scope and that scope is transferred to the logical
connection after handshake. The codec was still registered as a singleton,
allowing the factory to capture the root provider and making scoped encoder
dependencies unavailable during world entry.

## Decision

Register `IRaidoCodec<>` as scoped. The existing transient codec factory then
captures the active protocol scope, so encoder and decoder resolution remains
inside the same lifetime that owns the protocol. Existing singleton services
used by encoders remain unchanged.

The regression test enables scope validation, proves root resolution is
rejected, and proves an encoder with a scoped dependency works from a protocol
scope.

## Alternatives considered

- Making `ICharacterRenderMasksWriter` a singleton would hide the lifetime
  mismatch in one GameWorld service and leave other scoped protocol components
  broken.
- Creating a new scope for every encode would introduce a second lifetime
  owner and could outlive the protocol scope during connection writes.
- Resolving encoders from a global cache would not support scoped dependencies
  and would risk sharing connection-specific state.
