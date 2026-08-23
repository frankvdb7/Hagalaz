## ADDED Requirements

### Requirement: Fallow SHALL model the frontend's real entry points

The package-local Fallow configuration SHALL include the web, admin, launcher, test-provider, and development-proxy entry points that are invoked through Angular configuration or package scripts, while retaining analyzer rules for ordinary source files.

#### Scenario: Configuration-owned files are analyzed as reachable

- **WHEN** Fallow analyzes the frontend from its package root
- **THEN** `proxy.conf.mjs`, `src/test.ts`, and `src-admin/test.ts` are not reported as unused solely because Angular references them through `angular.json`

#### Scenario: Dynamic route entry points are retained

- **WHEN** Fallow analyzes the main and launcher route modules
- **THEN** the intentional repeated `routes` exports are treated as route entry points rather than duplicate dead API surface

### Requirement: Confirmed unreachable legacy frontend artifacts SHALL be removed

The frontend SHALL not retain source files, package scripts, or documentation for the unconfigured Protractor workflow or the unreachable legacy NgRx highscores implementation and helper files covered by this change.

#### Scenario: Existing frontend verification runs after cleanup

- **WHEN** the web and admin test/build commands run after cleanup
- **THEN** they complete without requiring the removed Protractor, NgRx, or helper artifacts

#### Scenario: No active product caller is changed

- **WHEN** the cleanup is applied
- **THEN** web routes, admin authentication, launcher startup, and existing Angular test providers retain their current behavior

### Requirement: Fallow exceptions SHALL be narrow and explainable

The Fallow configuration or source suppressions SHALL retain findings for ordinary code while excluding only observed framework lifecycle, dependency-injection, editor-tooling, Tailwind build, and stylesheet-resolution cases that static analysis cannot represent accurately.

#### Scenario: Legitimate tooling remains declared

- **WHEN** the dependency report runs
- **THEN** `@angular/language-service` and `tailwindcss` remain declared for their editor/build roles and are not treated as removable application runtime dependencies

#### Scenario: Valid stylesheet imports resolve or are explicitly bounded

- **WHEN** the stylesheet graph is analyzed and the web app is built
- **THEN** the OverlayScrollbars stylesheet resolves through its package export and Tailwind's documented package import does not create a false unresolved-import failure

### Requirement: The frontend quality workflow SHALL remain reproducible

The package SHALL expose the local Fallow audit command, and the existing frontend CI job SHALL run the same package-scoped changed-file audit with the locked dependency graph and sufficient Git history for comparison.

#### Scenario: Local audit uses the package-owned tool

- **WHEN** a developer runs `pnpm run fallow:audit` from `Hagalaz.Web.App`
- **THEN** the command uses the lockfile-resolved Fallow CLI and the checked-in configuration

#### Scenario: Pull-request audit is scoped to changed frontend code

- **WHEN** the frontend CI job runs for a pull request
- **THEN** Fallow audits changed files under `Hagalaz.Web.App` and does not require a second frontend analysis job
