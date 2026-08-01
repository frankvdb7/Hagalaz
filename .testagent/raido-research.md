# Raido coverage research

Target: `Raido.Common` and `Raido.Server`, with `Raido.Common.Tests` and `Raido.Server.Tests` as the test projects.

Conventions: MSTest 4, nullable-enabled C#  net10.0 projects, NSubstitute for isolated collaborators, and Coverlet Cobertura collection through `dotnet test --collect:"XPlat Code Coverage"`.

Acceptance checklist:

- Add tests for the Raido.Common protocol, buffer, message, and encoder behavior.
- Add tests for Raido.Server protocol readers/writers, pipe readers, connection lifecycle, dispatch, hub lifecycle, filters, codecs, lifetime management, proxies, options, builders, and extensions.
- Keep the existing test suites passing.
- Verify at least 80% line coverage for the combined Raido projects and each production project.
- Preserve a Cobertura report and a readable coverage summary.

Edge-case follow-up inventory:

- `MemoryBufferWriter`: empty destinations, zero-length writes, and byte-array offset/count handling.
- `ConsumableArrayBufferWriter`: consumed-prefix shifting and releasing oversized pooled arrays after exact consumption.
- `RaidoMessagePipeReader`: completed empty input, use after completion, and truncated completed input.
- `RaidoProtocolWriter`: cancellation before semaphore acquisition.

Usage-shaped Raido follow-up inventory:

- Registered protocol and codec path using an opcode plus length-prefixed payload, matching Hagalaz handshake/update protocols.
- Fluent `IRaidoConnectionContextBuilder` resolution of a registered protocol.
- Hub construction with a DI service, `RaidoMessageHandler`, `Context.Items`, and a message response written through the active protocol.
- Real `DefaultRaidoLifetimeManager` broadcast-except and targeted sends, matching Hagalaz client proxy usage.
