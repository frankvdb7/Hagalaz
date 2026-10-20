## 1. Shared range validation and application

- [x] 1.1 Refactor the existing range application path to run against either the real or an isolated slot array, preserving derived free-slot behavior for real containers; verify the focused container tests still compile.
- [x] 1.2 Replace the count-only `HasSpaceForRange` simulation with cloned-slot validation through the shared insertion rules, and ensure `AddRange` applies only after the complete range validates; verify failed ranges leave slots and counts unchanged.

## 2. Regression coverage

- [x] 2.1 Add MSTest coverage for non-stackable count capacity, same-range stack creation/extension, combined stack overflow, later-item failure, and `HasSpaceForRange`/`AddRange` agreement; verify contents and counts before and after failures.
- [x] 2.2 Run `dotnet test Hagalaz.Game.Abstractions.Tests/Hagalaz.Game.Abstractions.Tests.csproj --no-restore`, validate this OpenSpec change with `openspec validate make-add-range-atomic --type change --strict`, and review the final diff for excluded #437/#449 work.
