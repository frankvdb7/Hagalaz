## Why

The current package-local Fallow report has four health findings: two untested launcher/admin methods with high estimated CRAP and one admin component whose template rollup exceeds the cognitive-complexity threshold. The cleanup is complete, so these findings now need a focused behavior-preserving refactor rather than broad threshold suppression.

## What Changes

- Split the cache types page's read and mutation surfaces into focused standalone components so the parent and child templates remain below the Fallow cognitive-complexity threshold.
- Extract the shared cache-request error message mapping so the admin request runners have low cyclomatic complexity while preserving their loading/error lifecycle.
- Separate launcher command-shape validation from handler lookup so `LauncherApiHandler.getHandler` remains behaviorally identical but below the CRAP threshold.
- Add focused tests for command validation and cache error/request lifecycle behavior where the existing test setup can exercise the changed boundaries.
- Keep the existing Fallow thresholds and dependency graph; do not hide the findings with broad ignores or health threshold overrides.

## Capabilities

### New Capabilities

- `web-app-health`: The web application maintains Fallow health findings at zero for the current launcher and admin complexity scope without changing user-visible behavior.

### Modified Capabilities

None.

## Impact

- Affected code is limited to `Hagalaz.Web.App/src/launcher/launcher-api-handler.ts`, its focused tests, and `Hagalaz.Web.App/src-admin/app/features/cache/` page/service test files.
- The admin page markup is redistributed across standalone components; routes, service contracts, form controls, mutation endpoints, and launcher IPC command behavior remain unchanged.
- No new runtime dependency, CI worker, API, persistence, or deployment change is introduced.

## Scope and Acceptance Criteria

- Full Fallow health reports zero findings for the four current locations, with no threshold increase or broad health suppression.
- Fallow dead-code/dependency checks remain clean, and the changed-file audit passes.
- Existing web/admin tests and builds pass; focused tests cover the extracted launcher validation and cache request error/loading behavior.
- The final diff does not alter route paths, IPC channel names, command argument ordering, cache service calls, form fields, or mutation payloads.

## Non-Goals and Stop Conditions

- Do not add coverage infrastructure, change product behavior, redesign the cache API, or refactor unrelated admin pages.
- Stop and revise the design if splitting the page requires changing service contracts or causes a visible layout/interaction change that cannot be characterized by existing tests.
