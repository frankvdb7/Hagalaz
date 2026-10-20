## Context

`Hagalaz.Web.App` is a single pnpm project containing the Angular web application, admin application, and Electron launcher targets. Its direct dependencies currently mix Angular 21 patch levels and older frontend tooling. The package manifest is the source of declared intent, while `pnpm-lock.yaml` is the reproducible resolved graph.

The package manager and Angular CLI already own dependency installation and framework migrations. No new updater service, package manager, or compatibility abstraction is needed.

## Goals / Non-Goals

**Goals:**

- Move the Angular framework and CLI as one peer-compatible unit to the current Angular major available at implementation time.
- Update direct dependencies to current compatible releases and keep Node/TypeScript within Angular's published support range.
- Apply official Angular migrations and regenerate the lockfile using the repository's existing pnpm workflow.
- Prove the web, admin, test, lint, and launcher build paths that are affected by the dependency graph.

**Non-Goals:**

- Changing application behavior, backend contracts, or Electron runtime design.
- Introducing automated dependency update infrastructure.
- Forcing a package to a newest major when its peer requirements conflict with the selected Angular release.

## Decisions

### Angular CLI migrations own framework-major changes

Use the local Angular CLI's `ng update` migrations for the Angular major transition, then align the remaining Angular packages and compatible direct dependencies through pnpm. This preserves official schematics and avoids duplicating migration logic in ad hoc scripts.

The rejected alternative is only editing version strings and reinstalling: it would not apply source/configuration migrations and could leave Angular peers on mixed majors.

### `package.json` and `pnpm-lock.yaml` remain the dependency owners

The package manifest owns declared ranges and the lockfile owns the exact graph. A single pnpm update/installation operation updates both. The rejected alternative is adding a second lockfile or npm/yarn workflow, which would create competing sources of truth.

### Compatibility wins over the absolute newest package

Registry versions are inspected during implementation. Angular's supported Node and TypeScript ranges, plus peer dependencies of direct packages, determine the final versions. If the newest TypeScript or another dependency is incompatible, retain the newest compatible version and record the reason in the implementation outcome.

Stable releases take precedence over prereleases. `@ngrx/signals` has a stable 21.1.1 release with an Angular 21 peer range and a 22.0.0 release candidate; retain the stable release for this upgrade, verify its behavior against Angular 22, and record the peer metadata gap rather than promoting an RC into the production dependency graph.

The application uses Angular's zoneless change detection and has no ZoneJS imports. Disable pnpm's automatic optional-peer installation so Angular's Vitest builder does not discover and dynamically import an unused `zone.js/testing` package during test bundling.

### New diagnostics are fixed, not suppressed

Do not retain TypeScript `ignoreDeprecations` or Angular extended-diagnostic suppressions. Remove the deprecated `baseUrl` compiler option and fix imports that depended on it; allow Angular's nullish/optional-chain diagnostics to remain active so future compiler upgrades cannot hide real issues.

### Validation follows existing scripts and target topology

Run the existing web build, unit test, lint, admin build/test where supported by the shared graph, and launcher bundling/build checks. No new test harness is introduced. Build or test failures caused only by unavailable external services are reported separately from source or dependency failures.

## Risks / Trade-offs

- [Angular migration changes compiler or template diagnostics] → Apply official migrations, inspect the diff, and run build/test/lint before completion.
- [A direct package has no compatible Angular 22 release] → Keep the last compatible version, document the exception, and stop before adding a compatibility shim.
- [A stable package advertises an older Angular peer range] → Do not use a prerelease solely to clear metadata; retain the latest stable package only if installation and focused validation pass, and record the upstream gap.
- [The optional ZoneJS peer is auto-installed and breaks zoneless test bundling] → Disable automatic peer installation for this frontend workspace and verify that no application source imports ZoneJS.
- [A newest toolchain package requires a newer Node runtime than the project supports] → Update the declared engine only when it matches the repository's supported runtime policy; otherwise retain the newest compatible release and report it.
- [Lockfile resolution changes unrelated transitive packages] → Review the lockfile diff and retain only the dependency graph changes caused by this upgrade.
- [Rollback is needed after deployment] → Revert the manifest, lockfile, and migration diff together; no database or backend rollback is involved.

## Migration Plan

1. Capture current versions and verify the working tree is clean for the scoped files.
2. Query current registry versions and Angular compatibility metadata.
3. Apply Angular major migrations and update compatible direct dependencies.
4. Install with the pinned pnpm package manager to regenerate the lockfile.
5. Review the source/configuration diff, then run the focused frontend validation commands.
6. If validation fails due to an unresolved compatibility issue, revert the incomplete dependency change and record a follow-up rather than expanding scope.

## Open Questions

None at proposal time. Exact registry versions and any required migration edits are resolved during implementation and captured in the final validation report.
