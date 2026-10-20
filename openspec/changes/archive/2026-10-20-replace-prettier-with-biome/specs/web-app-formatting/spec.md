## ADDED Requirements

### Requirement: The web app SHALL expose a repository-owned Biome workflow

`Hagalaz.Web.App` SHALL provide package-manager commands that use the checked-in Biome configuration for formatting and linting supported frontend source files.

#### Scenario: Formatting check passes for compliant frontend files

- **WHEN** a developer runs the web app's formatting-check command from `Hagalaz.Web.App`
- **THEN** the command exits successfully when supported frontend files match the checked-in Biome formatting configuration

#### Scenario: Formatting check reports drift

- **WHEN** a supported frontend file does not match the checked-in Biome formatting configuration
- **THEN** the formatting-check command exits unsuccessfully and identifies the file requiring formatting

#### Scenario: Formatting write command is run

- **WHEN** a developer runs the web app's formatting-write command
- **THEN** Biome rewrites supported frontend files according to the repository configuration without changing application behavior

#### Scenario: Biome lint command is run

- **WHEN** a developer runs the web app's Biome lint command
- **THEN** Biome evaluates supported frontend source files and returns a non-zero exit status for lint violations

### Requirement: Frontend CI SHALL enforce the Biome lint workflow

The frontend `webapp` CI job SHALL run the package-local Biome lint command after installing the locked dependencies and before the frontend verification steps.

#### Scenario: Frontend CI validates lint

- **WHEN** the frontend CI job runs for a push or pull request
- **THEN** it installs the frontend dependencies from the lockfile and runs the Biome lint command from `Hagalaz.Web.App`

#### Scenario: Frontend lint violations are present in CI

- **WHEN** Biome reports a lint violation in the frontend CI job
- **THEN** the `webapp` job fails before reporting the frontend verification as successful

### Requirement: The web app SHALL use Biome as its direct formatter dependency

The web app SHALL declare Biome as a development dependency and SHALL NOT declare Prettier or the removed Tailwind-specific Prettier plugin as direct formatting dependencies.

#### Scenario: Frontend dependencies are installed from the lockfile

- **WHEN** a developer installs dependencies using the repository's declared package manager and lockfile
- **THEN** Biome is available to the web app's formatting and linting scripts
- **AND** the removed direct Prettier dependency chain is not required

#### Scenario: Legacy formatter configuration is inspected

- **WHEN** a developer searches the web app's formatter configuration
- **THEN** no `.prettierrc` configuration remains and the checked-in Biome configuration is authoritative

### Requirement: The Biome workflow SHALL preserve the established base style

The Biome configuration SHALL preserve four-space indentation, double-quoted JavaScript and TypeScript strings, semicolons, and final newlines to the extent supported by Biome.

#### Scenario: Existing style settings are applied

- **WHEN** Biome formats a supported TypeScript or JavaScript file
- **THEN** it uses four-space indentation, double quotes, semicolons, and a final newline

#### Scenario: Unsupported formatter-specific behavior is encountered

- **WHEN** an existing Prettier plugin behavior cannot be represented by Biome without an additional unsupported mechanism
- **THEN** the migration stops with that behavior identified as follow-up work rather than silently introducing a second formatter
