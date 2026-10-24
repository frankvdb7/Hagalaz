# Proposal

## Why

The web app's direct dependencies have newer stable releases than the versions recorded in its manifest, including compatible updates for its Angular 22 toolchain. Refreshing them keeps the web, admin, tests, lint, and Electron launcher on a current, reproducible dependency graph.

## What Changes

- Update direct runtime and development dependencies in `Hagalaz.Web.App/package.json` to the newest stable versions compatible with Angular 22 and the declared Node engine.
- Keep TypeScript on the newest Angular-supported 6.0 release; Angular 22.2.3 requires TypeScript `>=6.0 <6.1`, so TypeScript 7 is outside scope.
- Upgrade Biome to 2.6.2 and fix its new lint findings in source without suppressing or disabling any rules.
- Enable `noTailwindArbitraryValue` and `useTailwindShorthandClasses` as errors and fix every reported class without allowlists or lint suppressions.
- Set an app-scoped pnpm hoisted linker and point the Electron package entry at the existing launcher bundle so Forge 8 can package the app.
- Apply official package migrations where the Angular CLI or upgraded packages provide them, and regenerate `pnpm-lock.yaml` with the pinned pnpm workflow.
- Apply only the source and style changes needed to satisfy Biome 2.6.2 while preserving the app's current layout and behavior.
- Preserve application behavior and report any dependency that cannot be upgraded compatibly.

### Non-goals and stop conditions

- Do not change routes, UI features, API contracts, backend projects, or Electron runtime behavior.
- Do not suppress lint diagnostics, relax Biome rules, or pin Biome below 2.6.2 to avoid the new findings.
- Do not add package management tools, packages, or competing lockfiles.
- Stop and record a follow-up if a latest stable package conflicts with Angular 22 or the supported Node engine, or if an upgrade requires a feature redesign outside this toolchain refresh.
- Do not change pnpm configuration outside `Hagalaz.Web.App`.

### Acceptance Criteria

- `package.json` and `pnpm-lock.yaml` declare and resolve the newest stable compatible direct dependency versions, with Angular packages on one major and TypeScript in Angular's supported range.
- Biome 2.6.2 lint passes with its rules enabled and without suppressions.
- Tailwind class strings contain no arbitrary values and use shorthand utilities where equivalent.
- Existing web, admin, unit-test, lint, launcher build/bundles, and Forge packaging targets pass with the refreshed graph and app-scoped hoisted linker.
- The cumulative diff contains only the app's dependency/toolchain, linker, and package-entry changes and required migrations, with no application feature behavior changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The existing `web-app-toolchain` requirements already cover this tooling-only refresh.

## Impact

- `Hagalaz.Web.App/package.json`, `Hagalaz.Web.App/pnpm-lock.yaml`, and app-scoped pnpm/Biome/build-output configuration.
- App configuration or source only if an official package migration requires a focused compatibility edit.
- Existing frontend build, test, lint, admin, and launcher validation scripts.
