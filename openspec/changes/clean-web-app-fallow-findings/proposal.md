## Why

The newly enabled Fallow report for `Hagalaz.Web.App` mixes confirmed dead frontend code with stale Protractor scaffolding, one invalid stylesheet import, and findings caused by framework/configuration entry points that Fallow cannot infer. The report should become a trustworthy quality signal before it is used as a pull-request gate.

## What Changes

- Remove confirmed unused frontend files and declarations, including the abandoned NgRx highscores reducer/actions, unused router/assets/type helpers, and the unused theme index.
- Remove the obsolete Protractor end-to-end scaffold, its package script and stale README instructions; do not replace it with a new E2E framework in this change.
- Remove the unused `@angular/platform-server` dependency and retain legitimate editor/build tooling such as `@angular/language-service` and `tailwindcss` with explicit Fallow configuration where static analysis cannot observe their usage.
- Correct the OverlayScrollbars stylesheet import to the package's exported path.
- Make Fallow's entry points and intentional framework/tooling exceptions explicit, including Angular test providers, the dev proxy, dynamic route exports, and signal-based Angular/DI members.
- Remove the unused registration-form input and incomplete no-op output/listener contract, make internal-only types non-exported, and remove unused logger methods without changing product flows.
- **BREAKING**: the legacy `ng e2e`/Protractor scaffold is removed because it is not configured as an Angular target and has no working declared dependencies.

### Non-goals

- Do not change runtime APIs, authentication behavior, registration behavior, routing behavior, or the launcher protocol.
- Do not refactor the four existing health/complexity findings or introduce coverage collection; record those as follow-up work unless validation shows a cleanup change resolves one incidentally.
- Do not add a replacement E2E framework, new dependencies, generic suppression framework, or new CI worker.
- Do not delete files merely because Fallow cannot infer a framework entry point; preserve runtime/configuration files and model them as explicit entries or narrow suppressions.

## Capabilities

### New Capabilities

- `web-app-fallow-quality`: Defines the trusted, package-local Fallow analysis scope and the cleanup boundary for `Hagalaz.Web.App`.

### Modified Capabilities

- None. This change modifies development tooling and removes unreachable legacy scaffolding; it does not change a product/runtime requirement.

## Impact

- `Hagalaz.Web.App/.fallowrc.json`, `package.json`, `pnpm-lock.yaml`, source/config files, and the stale frontend README/E2E scaffold.
- Existing `.github/workflows/ci.yml` Fallow integration remains the single CI owner; no second analysis job is introduced.
- No backend services, public APIs, database schema, deployment topology, or persisted data are affected.

## Acceptance Criteria

- Fallow no longer reports the confirmed dead files/declarations, invalid OverlayScrollbars import, obsolete Protractor dependencies, or known Angular/configuration false positives within the scoped rules.
- `Hagalaz.Web.App` builds and both existing frontend test commands pass after the cleanup.
- `pnpm run lint:biome` and `pnpm run fallow:audit -- --base HEAD` pass from the frontend directory.
- The lockfile matches the final manifest, and the final diff contains no unrelated formatting or backend changes.
- Remaining health findings are explicitly reported as follow-up work rather than silently suppressed.

## Stop Conditions

- Stop if deleting a reported file reveals a production or test caller not represented in the current graph; restore the file and model the missing entry point instead.
- Stop if correcting the stylesheet import changes the Angular build output beyond resolving the missing package path.
- Stop and record a follow-up if resolving the health findings requires product behavior changes, broad refactoring, or a new coverage/CI mechanism.
