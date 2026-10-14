# Tasks

## 1. Explicit construction

- [x] 1.1 Replace the two internal completion-owner constructors with one boundary-taking constructor, preserve both public constructors, and migrate the five domain containers; verify every existing owner is attached to exactly one boundary and the old owner constructor calls are gone.
- [x] 1.2 Migrate the two owner-aware test compositions without changing their assertions; run `dotnet test Hagalaz.Game.Abstractions.Tests/Hagalaz.Game.Abstractions.Tests.csproj`, `dotnet test Hagalaz.Services.GameWorld.Tests/Hagalaz.Services.GameWorld.Tests.csproj`, and `dotnet test Hagalaz.Game.Scripts.Tests/Hagalaz.Game.Scripts.Tests.csproj`.

## 2. Integration validation

- [x] 2.1 Review the cumulative affected diff and verify no transaction, storage, boundary, publication, public API, or gameplay behavior changed; run `dotnet build Hagalaz.sln`, `openspec validate --all --strict`, the repository duplication check, and `git diff --check`.
