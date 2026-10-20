## Context

`Hagalaz.Web.App` is an Angular/Electron frontend with a local `pnpm-lock.yaml`, an existing `.prettierrc`, and direct Prettier dependencies. The current configuration establishes four-space indentation, double quotes, semicolons, and ES5 trailing commas, while the Tailwind Prettier plugin adds class ordering. There are no existing frontend format scripts, so the migration must establish the explicit package workflow as part of the replacement.

The change is limited to frontend development tooling. It has no runtime state transitions, service ownership, API contract, or persistence behavior; package.json and the checked-in Biome configuration are the single owners of the workflow.

## Goals / Non-Goals

**Goals:**

- Make Biome the only direct formatter dependency for `Hagalaz.Web.App`.
- Provide explicit package scripts for formatting, format checking, and Biome linting while retaining Angular's existing `lint` script unless the implementation proves it is obsolete.
- Preserve the existing base formatting style where Biome supports it.
- Remove the obsolete Prettier configuration, direct dependencies, and explicit editor/devcontainer references.
- Keep the lockfile reproducible and validate the new commands against the actual frontend workspace.
- Make the existing frontend CI job invoke the same package-local Biome lint command used by developers.

**Non-Goals:**

- Do not change Angular, TypeScript, Electron, test, build, or runtime behavior.
- Do not rework the existing Angular lint configuration or add a generic lint orchestration layer.
- Do not retain Tailwind class sorting through a second formatter or an unmaintained compatibility bridge.
- Do not reformat unrelated backend code.

## Decisions

### Use the package-local Biome CLI

Declare `@biomejs/biome` in `Hagalaz.Web.App`'s devDependencies and invoke it through package scripts. This makes the lockfile and package manager the authoritative dependency source and avoids relying on a globally installed executable.

Alternatives considered:

- Keep invoking Prettier through `npx`: rejected because it leaves the removed formatter as an implicit dependency and does not provide a reproducible local toolchain.
- Use an editor-only Biome installation: rejected because CI and command-line validation need the same formatter.

### Use one checked-in Biome configuration and focused scripts

Add a Biome configuration at the frontend root and expose separate write, check, and lint commands. The write command will be the only mutating formatter operation; the check and lint commands will be suitable for CI or local verification. Angular's existing `ng lint Hagalaz.Web.App` remains the existing `lint` entry point, with a named Biome lint script added alongside it to avoid silently changing its behavior.

Alternatives considered:

- Replace the existing `lint` script immediately: rejected because Angular lint and Biome lint have different rule ownership, and changing the existing command would expand the migration beyond formatter replacement.
- Add a generic root-level task runner: rejected because one package's scripts are sufficient and a second orchestration mechanism would add no value.

### Treat unsupported Prettier plugin behavior as an explicit boundary

Biome's native formatting is authoritative. The Tailwind Prettier plugin is removed rather than wrapped or run after Biome. If validation shows that required Angular/template or other supported-source syntax cannot be handled safely, the implementation stops and records the exact gap as follow-up work.

Alternatives considered:

- Keep the Tailwind plugin beside Biome: rejected because it preserves a second formatter and undermines the requested replacement.
- Apply an automated whole-repository reformat without review: rejected because formatting drift is a toolchain migration concern, not a reason to alter unrelated source files broadly.

### Enforce Biome lint in the existing frontend CI job

Add one step to the existing `.github/workflows/ci.yml` `webapp` job immediately after `pnpm install --frozen-lockfile`. The step runs the package script rather than a globally resolved executable, so local development and CI share the same Biome version and configuration. The existing frontend tests and builds remain unchanged and continue after linting.

Alternatives considered:

- Create a separate CI job: rejected because the existing `webapp` job already owns frontend dependency installation and verification, and a second job would duplicate setup.
- Run Biome through `pnpm dlx`: rejected because it could resolve a different version than the lockfile-managed package.

### Update the shared devcontainer editor extensions only where they explicitly select Prettier

Remove duplicate Prettier extension entries from `.devcontainer/devcontainer.json` and add the Biome extension if the repository's editor setup supports it. No application code or unrelated editor settings change.

## Risks / Trade-offs

- [Formatting differences] → Run Biome in check mode, inspect the resulting scope, and keep style settings aligned with the current `.prettierrc`; report any broad or behavior-sensitive changes rather than silently accepting them.
- [Angular template or stylesheet coverage] → Validate the real frontend file set before finalizing the include scope; stop with a follow-up if Biome cannot safely format a required file type.
- [Loss of Tailwind class ordering] → Make the removal explicit in the proposal and do not introduce a second formatter; Tailwind's CSS build remains unchanged.
- [Editor drift] → Keep the Biome configuration and package scripts in the repository and update explicit devcontainer formatter extensions.

## Migration Plan

1. Add the package-local Biome dependency, configuration, and format/lint scripts.
2. Remove Prettier dependencies and `.prettierrc`; update the frontend lockfile and explicit devcontainer references.
3. Add the Biome lint step to the existing frontend CI job.
4. Run the Biome check/lint commands and inspect any proposed formatting changes.
5. Run the existing frontend build and test commands, then review the final diff for unrelated formatting churn.

Rollback is a single revert of the toolchain change: restore `.prettierrc`, the two Prettier dependencies, their lockfile entries, and the previous devcontainer extension entries. No runtime data migration or deployment rollback is required.

## Open Questions

Resolved during implementation: Biome 2.5.8 formats the configured TypeScript, HTML, CSS, SCSS, JavaScript, and JSON files, with Angular interpolation and parameter decorators enabled in the parser configuration. Generated output, editor files, and the legacy e2e directory remain outside the formatter scope.
