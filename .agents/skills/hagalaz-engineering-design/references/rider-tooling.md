# Rider tooling for C# work

Use Rider's semantic tools for C# instead of grep, `dotnet build` or `dotnet test` whenever Rider can answer. Fall back to text search or the CLI only when a Rider tool cannot answer, and say so in the report.

| Need | Rider tool / skill |
|---|---|
| Where is a symbol declared | `search_symbol` (skill `rider-skills:navigating-code`) |
| Who uses / implements a symbol | `find_usages`, `find_implementations` (`rider-skills:navigating-code`) — run before changing a type or member |
| Existing tests for a class/method | Search the relevant test projects with `rg`. Rider's `findTests` coverage lookup has launched unrelated Hagalaz suites and left test hosts running. |
| Rename, move, change signature, extract, delete | `rename_refactoring`, `move_type_to_namespace`, `change_api_signature`, `extract_*`, `safe_delete` (`rider-skills:refactoring-code`) |
| Compile errors / warnings in edited files | `get_file_problems`, `build_solution_start` / `build_solution_state` |
| Run tests | `test_run_start` / `test_run_state` |
| Runtime state | `xdebug_*` (`rider-skills:debugging-code`) |

Load a skill by its full name (for example `Skill` with `rider-skills:refactoring-code`) even when it is missing from the skill list; the listing can omit it.

## Preconditions

1. **Pass `rootFolder`** (the directory that holds `Hagalaz.sln`) on every call. Without it Rider fails with `Unable to determine the target project`.
2. **Rider only sees projects that are open in it, and it opens each git worktree as its own project.** The error above lists the open project paths. If the working directory is a git worktree that is not open in Rider, Rider answers for a *different checkout and branch*: results and `get_file_problems` do not reflect your edits.
   - Rider's docs advise against worktrees nested inside the main project's folder (for example `<repo>/.claude/worktrees/...`); such a worktree is not indexed as its own project and calls resolve to the main checkout.
   - Use Rider for a worktree only when the worktree folder itself appears in the open-projects list. Then pass that path as `rootFolder`.
   - Otherwise (the usual case for agent worktrees) use text search and the CLI for that session, and say so in the report. Do not use Rider results for edits made in a worktree.
3. Check once that the answer belongs to your checkout: a `search_symbol` hit for a type you just added or edited must be found under your `rootFolder`. If it is not, stop using Rider for the session.
4. **Deferred tools (Claude Code):** Rider MCP tools may be deferred. Load every one you will need in a single `ToolSearch` call, for example `select:mcp__rider__search_symbol,mcp__rider__find_usages,mcp__rider__find_implementations,mcp__rider__get_file_problems,mcp__rider__build_solution_start,mcp__rider__build_solution_state,mcp__rider__test_run_start,mcp__rider__test_run_state,mcp__rider__safe_delete,mcp__rider__rename_refactoring`.
5. Rider's index can lag while it is indexing: a `note` in the result means the list may be incomplete.

## Verification order

Run focused Rider tests (`test_run_start`) and `get_file_problems` on edited files first. Run a Rider solution build, or the CLI examples in [context-efficient-commands.md](context-efficient-commands.md), when Rider is unavailable for the checkout or when the CLI result is required (for example CI parity).
