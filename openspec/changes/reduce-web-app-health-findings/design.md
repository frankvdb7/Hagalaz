## Context

Fallow currently reports four health findings in the frontend: `LauncherApiHandler.getHandler` has estimated CRAP 72, the sprite and types page `run` methods each have estimated CRAP 56, and the types page template/class rollup exceeds the cognitive-complexity threshold. The findings are caused by concentrated validation/error plumbing and one large Angular template; the existing routes, services, forms, and IPC registrations are otherwise valid and must remain the runtime owners.

## Goals / Non-Goals

**Goals:**

- Reduce the four reported health findings through small, behavior-preserving boundaries.
- Keep one shared cache error-message mapping for the admin request lifecycle.
- Keep the existing page services as the owners of HTTP/cache operations and keep `LauncherApiHandler` as the owner of IPC dispatch.
- Add focused characterization tests for invalid launcher commands and cache request success/failure state transitions.

**Non-Goals:**

- No Fallow threshold changes, broad health ignores, coverage-infrastructure change, route/API change, or new dependency.
- No changes to command argument ordering, IPC channel names, cache service methods, form controls, mutation payloads, or user-visible copy.
- No refactor of unrelated admin pages or launcher handlers.

## Decisions

### Extract one cache error mapper and reuse it in both page runners

Create one small cache-domain helper that preserves the current precedence (`error.detail`, `error.title`, `message`, fallback text). The sprite page and the types read/mutation components will call it from their existing `catch` blocks, leaving `run` responsible only for clearing the error, setting loading, executing the action, mapping failure, and resetting loading.

Alternatives considered:

- Raise the CRAP threshold: rejected because it hides the same error-handling complexity in future cache pages.
- Duplicate a simplified mapper in each component: rejected because the precedence rule would have multiple owners.
- Add a general application-wide error framework: rejected because only the cache pages share this contract and the current helper is sufficient.

### Split the types page by user-facing responsibility

Move the existing read cards/forms into a standalone read panel and the mutation forge into a standalone mutation panel. Each child owns the forms, signals, service calls, and loading/error state for its surface; the route-level page remains a composition shell. This reduces the template rollup without changing the forms or endpoint calls.

Alternatives considered:

- Suppress the component or raise its cognitive threshold: rejected because it would hide template growth.
- Move markup into `ng-template` blocks: rejected because Fallow would still analyze one large template and the responsibility boundary would remain unclear.
- Split every card into a component: rejected as unnecessary component proliferation; two cohesive surfaces are the smallest meaningful boundary.

### Extract launcher command-shape validation from handler lookup

Keep `getHandler` responsible for consuming the command argument, logging invalid input, looking up the registered handler, and returning it. Move the object/`commandType` shape predicate into a private type guard so the lookup method falls below the CRAP threshold while preserving the existing warning categories and the mutation of the argument list before handler invocation.

Alternatives considered:

- Change the public launcher command type or add a second dispatch path: rejected because the existing IPC protocol is the runtime contract.
- Remove validation: rejected because invalid renderer input must continue to be rejected and logged.
- Add a generic command-dispatch abstraction: rejected because the current map and two IPC callbacks already provide the needed mechanism.

### Test the preserved boundaries through existing Vitest setup

Add focused Vitest specs using the current Angular/Electron test tooling. Exercise invalid/missing/unknown launcher commands through the registered IPC callback and exercise cache request success/failure loading/error transitions with mocked services. These tests characterize behavior; the health reduction comes from the code boundaries, not from changing Fallow's coverage model.

## Risks / Trade-offs

- [Child component extraction changes Angular template wiring] → Keep the same form bindings, event handlers, service calls, and visible markup; run admin tests and production build.
- [A helper changes error precedence] → Preserve the existing expression exactly and cover detail/title/message/fallback cases.
- [Launcher refactor changes which arguments reach handlers] → Test a valid command with trailing arguments and assert the handler receives the same trailing argument list.
- [Fallow reports a new child-template hotspot] → Run the full health report before completion; if a new finding appears, keep the split to the smallest cohesive surface or revise the design rather than suppressing it.

## Migration Plan

1. Add the cache error mapper and launcher validation helper with focused tests.
2. Extract the types read and mutation panels, preserving their existing templates and service/form ownership.
3. Run Fallow, Biome, web/admin tests, and web/admin builds.
4. Rollback is a revert of this change; no persisted data or deployment state is affected.

## Open Questions

None. The current findings and their exact source locations define the scope.
