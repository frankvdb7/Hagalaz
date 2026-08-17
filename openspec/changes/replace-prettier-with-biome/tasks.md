## 1. Configure the frontend formatter

- [x] 1.1 Add the package-local Biome dependency and checked-in configuration for the supported `Hagalaz.Web.App` source files, preserving the established base style.
- [x] 1.2 Add explicit package scripts for Biome formatting write, formatting check, and linting while retaining the existing Angular lint command.

## 2. Remove the legacy formatter

- [x] 2.1 Remove the direct Prettier and Tailwind Prettier plugin dependencies, delete `.prettierrc`, and regenerate the frontend lockfile.
- [x] 2.2 Remove duplicate Prettier editor extensions from the shared devcontainer and add the Biome editor extension where appropriate.
- [x] 2.3 Add the package-local Biome lint command to the existing frontend `webapp` CI job after locked dependency installation.

## 3. Validate the migration

- [x] 3.1 Run Biome formatting check and lint commands, resolve only migration-scoped findings, and verify no required file type is left without safe coverage.
- [x] 3.2 Run the existing `Hagalaz.Web.App` build and test commands, verify the CI workflow references the Biome lint script, search for remaining direct Prettier/configuration references, and review the diff for unrelated source churn.
