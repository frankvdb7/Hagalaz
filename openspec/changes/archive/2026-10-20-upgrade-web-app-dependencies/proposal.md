## Why

`Hagalaz.Web.App` is pinned to a mixed Angular 21 dependency graph while newer Angular and frontend packages are available. Updating the web toolchain now keeps the application on a supported Angular major and removes avoidable drift between framework peers and the rest of the package graph.

## What Changes

- Upgrade all Angular framework, CLI/build, compiler, Material, CDK, and service-worker packages to one current Angular major, including the Angular migration schematics.
- Upgrade the remaining direct frontend dependencies to their current registry versions where they remain compatible with the Angular 22 toolchain.
- Keep TypeScript and Node engine declarations within Angular's supported compatibility range rather than taking incompatible latest majors.
- Regenerate `pnpm-lock.yaml` from the updated `package.json` and preserve the existing pnpm package-manager workflow.
- **BREAKING**: Apply any source/configuration migrations required by Angular 22 and update frontend validation commands if the upgraded CLI requires it.
- Resolve newly surfaced compiler and template diagnostics in source/configuration instead of suppressing them.
- Validate the web app with its production build, unit tests, and lint command; document any environment-only limitation.

### Non-goals and stop conditions

- Do not change application features, routes, API contracts, Electron behavior, backend projects, or unrelated repository dependencies.
- Do not add a second package manager, dependency-management tool, or custom compatibility layer.
- Do not adopt prerelease packages to mask a peer-range gap. If an upstream package has no stable Angular 22-compatible release, retain its latest stable version only when the install and focused validation pass, and record the upstream compatibility gap for follow-up.

## Capabilities

### New Capabilities

- `web-app-toolchain`: Maintain a reproducible, Angular-supported dependency baseline for the web application and its Electron build targets.

### Modified Capabilities

<!-- No existing runtime requirement changes; this is a toolchain and reproducibility change. -->

## Impact

- `Hagalaz.Web.App/package.json` and `Hagalaz.Web.App/pnpm-lock.yaml`.
- Angular application source/configuration touched by official Angular 22 migrations, if any.
- Frontend build, test, lint, and Electron bundling validation through the existing package scripts.
- No backend APIs, persisted data, or deployed service contracts.
