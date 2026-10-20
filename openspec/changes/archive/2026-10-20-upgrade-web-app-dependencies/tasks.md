## 1. Inventory and compatibility

- [x] 1.1 Capture the current frontend dependency graph, Angular migrations, and working-tree scope.
- [x] 1.2 Query current registry versions and Angular Node/TypeScript/peer compatibility; select the newest compatible set.

## 2. Upgrade

- [x] 2.1 Apply the official Angular major upgrade and update all Angular framework/tooling/Material packages together.
- [x] 2.2 Update compatible direct frontend dependencies, package-manager metadata, and the pnpm lockfile.
- [x] 2.3 Inspect and correct only source/configuration changes required by the Angular migrations.

## 3. Validate

- [x] 3.1 Run the web production build, unit tests, and lint checks.
- [x] 3.2 Run the admin and Electron launcher build/bundle checks supported by the environment.
- [x] 3.3 Review the final diff for scope, peer consistency, reproducibility, and documented limitations.

## 4. Resolve compiler diagnostics

- [x] 4.1 Remove Angular and TypeScript diagnostic suppressions and the deprecated `baseUrl` compiler option.
- [x] 4.2 Fix the import exposed by removing `baseUrl`, then rerun the web/admin builds and test suites.
