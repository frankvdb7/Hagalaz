# Raido coverage status

Completed:

- Added focused tests across the existing Raido test projects: 36 Common tests and 62 Server tests pass in the final run.
- Covered protocol read/write paths, pipe cancellation/backlog, connection abort/write/timeout behavior, hub dispatch/lifecycle/filter/auth behavior, reflection executors, codec registration, lifetime/proxy dispatch, options/builders, metrics, and extensions.
- Changed `coverlet.collector` from an ineffective `Update` reference to an actual `Include` reference in both test projects and refreshed their lock files.

Final verification:

- `dotnet test Raido.Common.Tests\\Raido.Common.Tests.csproj --no-restore --collect:"XPlat Code Coverage" ...`: 36 passed, 1 existing ignored test.
- `dotnet test Raido.Server.Tests\\Raido.Server.Tests.csproj --no-restore --collect:"XPlat Code Coverage" ...`: 62 passed.
- Combined line coverage: 81.1% (2,155/2,655); `Raido.Common`: 85.9%; `Raido.Server`: 80.5%.
- Combined branch coverage: 69.0% (483/700).
- CRAP analysis flagged 9 methods above 30; the largest remaining hotspot is `DefaultRaidoHubDispatcher.StartActivity`.

Edge-case follow-up verification:

- Added `MemoryBufferWriterEdgeCaseTests`: 3 focused tests pass.
- Added `RaidoEdgeCaseTests`: 6 focused tests pass.
- Fixed empty `MemoryBufferWriter.CopyTo` handling and completed-input classification in `RaidoMessagePipeReader`.
- Full suites: Common 39 passed, 1 existing ignored; Server 68 passed.
- Fresh combined coverage: 82.6% line (2,196/2,658), Common 92.1%, Server 81.4%; branch coverage 71.4% (500/700).
- Report: `TestResults\\raido-edge-final-report\\Summary.txt` and `Cobertura.xml`.

Usage-shaped follow-up verification:

- Added `Raido.Common.Tests/RaidoHagalazUsageTests.cs`: 2 tests pass.
- Added `Raido.Server.Tests/RaidoHagalazUsageTests.cs`: 3 tests pass.
- Full suites: Common 41 passed, 1 existing ignored; Server 71 passed.
- Fresh combined coverage: 82.8% line (2,202/2,658), Common 89.4%, Server 82.0%; branch coverage 71.4% (500/700).
- Report: `TestResults\\raido-hagalaz-usage-report\\Summary.txt` and `Cobertura.xml`.
