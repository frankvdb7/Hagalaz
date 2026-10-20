## Why

`Hagalaz.Web.App` currently carries Prettier and a Tailwind-specific Prettier plugin without exposing a package script for formatting. Replace that formatter with Biome so the frontend has one maintained, package-local formatter and linter entry point with configuration checked into the repository.

## What Changes

- Add Biome as the frontend development formatter/linter dependency and configure it for the existing TypeScript, HTML, CSS, JSON, and related frontend source files.
- Add explicit package scripts for checking and writing formatting, plus linting through Biome.
- Run the Biome lint command in the frontend `webapp` CI job after dependency installation.
- Remove the Prettier dependency, the Tailwind Prettier plugin, and the obsolete `.prettierrc` configuration.
- Update the frontend lockfile and developer documentation/configuration references needed to use Biome.
- **BREAKING**: frontend formatting commands and formatting output are owned by Biome instead of Prettier; the removed Prettier-specific Tailwind ordering plugin is not retained as a separate formatter.

### Non-goals

- Do not change Angular build, test, runtime, or application behavior.
- Do not replace Angular's existing lint configuration or introduce a second linting framework beyond the Biome entry point required by this change.
- Do not reformat unrelated backend files or perform broad source cleanup unrelated to establishing the Biome workflow.

### Acceptance Criteria

- `Hagalaz.Web.App` has no direct Prettier package, Prettier plugin package, or `.prettierrc` configuration remaining.
- The package exposes deterministic Biome check, write, and lint commands that can run from the frontend directory.
- Biome configuration preserves the repository's existing four-space indentation, double-quoted TypeScript style, semicolons, and final-newline expectations where Biome supports those settings.
- The frontend dependency lockfile resolves the declared Biome dependency and no longer contains the removed direct Prettier dependency chain.
- The Biome check/lint commands and the existing frontend build/test commands pass, or any environment-only limitation is reported separately.
- The frontend CI job runs the Biome lint command from `Hagalaz.Web.App` and fails when Biome reports lint violations.

### Stop Conditions

- Stop and record a follow-up if preserving Tailwind class ordering requires a new formatter/plugin mechanism outside Biome's supported configuration.
- Stop and record a follow-up if Biome cannot cover a file type currently formatted by the repository without changing application behavior.

## Capabilities

### New Capabilities

- `web-app-formatting`: Defines the repository-owned Biome formatting and linting workflow for `Hagalaz.Web.App`.

### Modified Capabilities

- None. No existing runtime or product requirement changes.

## Impact

- `Hagalaz.Web.App/package.json`, its package manager lockfile, and a new Biome configuration file.
- `.github/workflows/ci.yml`, specifically the existing frontend `webapp` job.
- Removal of `Hagalaz.Web.App/.prettierrc` and the two direct Prettier development dependencies.
- Any frontend developer documentation or editor/devcontainer formatter references that explicitly select Prettier.
- No backend services, public APIs, database schema, or deployed runtime components.
