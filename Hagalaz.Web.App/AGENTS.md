# Hagalaz frontend agent guide

This guide applies to all files under Hagalaz.Web.App. Follow the repository-wide instructions in [../AGENTS.md](../AGENTS.md) and its OpenSpec workflow.

## Find the affected application

- Public Angular app: `src/`, bootstrapped by `src/main.ts`.
- Admin Angular app: `src-admin/`, bootstrapped by `src-admin/main.ts`. Cache tools live under `app/features/cache/`.
- Electron main process, preload, and commands: `src/launcher/`. Keep browser and Electron process boundaries explicit.
- Shared Material palettes and fonts: `src/theming/`. Each Angular app has its own global stylesheet.

Use `angular.json` to resolve project targets and `package.json` for installed versions, Node requirements, scripts, and the pinned pnpm version. Run frontend commands from this directory.

## Angular and state

- Before Angular implementation, consult [.agents/skills/angular-developer/SKILL.md](.agents/skills/angular-developer/SKILL.md). When the Angular MCP server is available, use its project discovery and best-practice guidance.
- Use standalone components, signal forms, signal-based inputs and outputs, native template control flow, and `inject()`. Follow the installed Angular version's defaults for change detection and zoneless operation.
- Use NgRx signal stores for feature data, request operations, loading, and errors. Keep one owner for each state value and update store state through `patchState`.
- Keep form models, field validation, file inputs, and browser resource cleanup in components. Pass validated values to store commands; HTTP services remain responsible for endpoint calls.
- Provide page-specific stores at the component or route boundary. Use application-wide stores only for state that must survive navigation, such as the signed-in user.
- The admin Types and Sprites stores under `src-admin/app/features/cache/stores/` demonstrate page-scoped request ownership. Preserve pending-request guards, useful error messages, and preview URL cleanup.
- Preserve `withExperimentalPlatformNavigation()` and `withAutoCleanupInjectors()` in both router bootstraps. Use the supported, non-deprecated injector-cleanup API.

## Material UI and styling

- Before designing or changing UI/UX, read [DESIGN.md](DESIGN.md) and follow its visual identity, component patterns, layout, and accessibility guidance for the affected application.
- Use Angular Material as the visual foundation. Reuse existing components, palettes, and typography before introducing another visual treatment.
- Use Tailwind utilities for layout, spacing, sizing, and responsive behavior. Set Material appearance through its public Sass theme and component override APIs.
- Keep color and typography customization in SCSS using Material design tokens and overrides. Do not put CSS custom properties or token expressions in HTML.
- Do not style internal MDC classes, override private Material selectors, or use `!important` to force component appearance.
- Use sentence-case labels and explicit action names. Provide inline validation, accessible loading/error/success feedback, and useful empty states.
- Check keyboard operation and narrow layouts. Keep wide result content scrollable within its own control rather than overflowing the page.

## Tooling and packaging

- Use pnpm and preserve the hoisted node linker in `.npmrc`. Electron Forge packaging requires dependencies on disk.
- Biome owns linting and formatting. Follow `biome.json` and `.editorconfig`; both Tailwind arbitrary-value and shorthand rules are errors.
- Fix findings at their source. Do not weaken lint, Fallow, type checking, CSP, or authorization to make a check pass.
- Format only affected files. Check the file list before a command that writes files; an empty selection must not become a project-wide formatter run.
- Use the installed Fallow tooling for code-health analysis. Avoid copying plugin-provided skills into the repository.

## Tests and code coverage

- Treat automated tests and coverage as completion criteria for behavior changes and refactors. Add or update regression tests for the affected behavior, then run the affected application's test target.
- Test observable outcomes rather than private implementation details. Cover successful operations, realistic failures, validation boundaries, and lifecycle cleanup. A regression test should fail when the behavior it protects is broken.
- Test changed NgRx stores directly, including results, loading/error transitions, pending-request guards, and mutations. Cover component integration and resource cleanup where those responsibilities remain in the page. Passing HTTP service tests alone does not verify store or component behavior.
- Use Angular HTTP testing utilities and Material component harnesses where appropriate. Keep asynchronous tests deterministic and verify production-reachable states.
- Collect coverage for changed code and inspect uncovered lines, branches, functions, and statements. Cover meaningful failure and boundary paths as well as the happy path. Report measured coverage and remaining gaps; do not infer coverage from a passing test count.
- Preserve configured coverage thresholds and improve coverage of changed code. Do not lower thresholds, exclude production code, or add superficial assertions to inflate the result. If coverage cannot be collected, report the concrete blocker and what remains unverified.
- Browser checks complement automated tests and coverage. They do not replace regression tests for application logic.

## Verification commands

Choose commands for the affected runtime:

| Runtime | Build | Existing tests |
| --- | --- | --- |
| Public app | `pnpm run build` | `pnpm test` |
| Admin app | `pnpm run admin:build` | `pnpm run admin:test` |
| Electron launcher | `pnpm run launcher:build` | Select relevant checks from `package.json` |

Use `pnpm start`, `pnpm run admin:serve`, or `pnpm run launcher:start` for the corresponding development runtime. Run `pnpm run lint:biome` for frontend changes and `pnpm run fallow:audit` before delivery of code changes.

For UI changes, inspect the actual affected app in a browser at desktop and phone widths and exercise the changed flows. Keep fixtures and screenshots outside committed source. Report fixture-backed verification separately from live authentication and backend persistence.

For documentation-only changes, verify paths, commands, and whitespace. Report existing warnings separately from warnings introduced by the change.
