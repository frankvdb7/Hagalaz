# Design

## Context

See `proposal.md` for the motivation and scope. `Hagalaz.Web.App` is a single pnpm project with Angular web/admin targets and an Electron launcher. `package.json` declares direct dependency ranges; `pnpm-lock.yaml` records their reproducible graph. The Angular 22.2.3 registry metadata requires Node `^22.22.3 || ^24.15.0 || >=26.0.0` and TypeScript `>=6.0 <6.1`; the checked-in Node engine range and installed Node 24.16.0 satisfy those constraints.

The current manifest also uses NgRx SignalStore and `@ngrx/operators`. NgRx 22 provides an update migration for the package group containing both packages. Electron Forge 8 is stable and ESM-only, while this package is already `type: module` and its Forge config currently uses CommonJS assignment syntax.

The Forge package check initially stopped because pnpm used its isolated linker. Forge crawls on-disk dependencies and requires pnpm's hoisted layout. The app has its own `pnpm-workspace.yaml`, so the linker setting belongs in `Hagalaz.Web.App/.npmrc`. After that preflight passed, Forge reported that `package.json` had no valid `main`; the launcher bundle scripts already produce `dist-launcher/main.js`, so use that as the package entry. Forge packaging succeeds with the app-scoped linker and built main/preload bundles.

## Goals / Non-Goals

**Goals:**

- Refresh direct packages from registry metadata while retaining peer and engine compatibility.
- Use existing pnpm, Angular CLI, and NgRx migrations as the dependency update mechanisms.
- Upgrade Biome to 2.6.2 and fix all reported findings in source without suppressions, relaxed rules, or configuration workarounds.
- Enable Biome's `noTailwindArbitraryValue` and `useTailwindShorthandClasses` rules at error severity; replace findings with existing utilities, explicit component styles, or named theme utilities without rule exceptions.
- Keep behavior in the Forge config unchanged while making its module export compatible with Forge 8.
- Use pnpm's hoisted linker only for the app workspace and set the package entry to its existing launcher bundle so Forge packaging completes.
- Verify the existing web, admin, unit-test, lint, and launcher targets after installation.

**Non-Goals:**

- Change app features, Electron runtime behavior, or backend projects.
- Change the Node engine declaration or take TypeScript 7 while Angular 22 requires TypeScript below 6.1.
- Broaden the lint config, suppress the new Biome diagnostics, or make unrelated UI/style changes to accommodate the newer linter.
- Add packages, update automation, or introduce another package manager or lockfile.
- Change pnpm configuration outside `Hagalaz.Web.App`.

## Decisions

### Use upstream migrations for framework package groups

Update Angular through the installed Angular CLI so its framework and Material/CDK migrations run with their package changes. Update NgRx through its published package-group migration so `@ngrx/signals` and `@ngrx/operators` remain aligned. Use pnpm for the remaining direct dependencies and the lockfile.

The simpler alternative, editing package version fields and reinstalling only, would skip migrations specifically published by Angular and NgRx. No new migration framework is needed.

### Keep TypeScript within Angular's peer range

Use the newest stable TypeScript release accepted by Angular 22.2, retaining `~6.0.3` while TypeScript 7 is outside Angular's declared peer range. Keep the existing Node engine declaration because its supported Node 24 range includes the installed Node version and satisfies Angular and Forge requirements.

### Upgrade Biome and resolve its findings

Use Biome 2.6.2 and keep every lint rule enabled. Replace Angular `::ng-deep` styles with selectors scoped to the existing `admin-login-page` and `cache-shell-page` host elements in the admin global stylesheet. Move the overlayscrollbars CSS from the end of `src/styles.scss` into the Angular global styles list after that stylesheet, preserving load order. Use an equivalent RGB notation for the flagged color, add visually hidden `Home` text to the logo link, and provide a generic font fallback for Material Symbols.

Enable `noTailwindArbitraryValue` and `useTailwindShorthandClasses` as errors with their default strict settings. Replace arbitrary Tailwind values with utilities from the existing theme, named theme tokens/utilities where the design needs a custom value, and focused component CSS for one-off layout values. Replace equivalent pairs such as width/height with their Tailwind shorthand. Do not add allowed classes/categories or lint suppressions.

### Preserve the Forge config's behavior and export it as ESM

Change `forge.config.js` to use `export default` with the same configuration object. This matches the package's `type: module` and Forge 8's ESM-only loading. The config has no custom maker, publisher, or plugin APIs to migrate.

Set the package `main` entry to the existing bundled Electron entry, `dist-launcher/main.js`, so Forge's package command can resolve the app entry after the launcher main/preload bundles are built.

Set `node-linker=hoisted` in `Hagalaz.Web.App/.npmrc`. Electron Forge's packager crawls the app's `node_modules` directory and does not resolve pnpm's default symlinked layout. The hoisted linker gives Forge the on-disk dependency layout it expects. Keep this setting scoped to the app's pnpm workspace and verify packaging with the pinned pnpm version.

Ignore Forge's generated `out/` package directory in Git and Biome, alongside the existing build-output exclusions, so copied app files and their embedded config do not enter source linting.

Electron 45 removes several Node builtins from sandboxed preload scripts. The existing preload only imports Electron APIs and local command modules, so no preload compatibility code is added.

### Keep package state in the existing manifest and lockfile

`package.json` remains the source for direct dependency intent, `.npmrc` owns the workspace's pnpm linker setting, and the pinned pnpm version generates `pnpm-lock.yaml`. Manual lockfile edits, another lockfile, or a new update tool would add competing owners without helping this refresh.

## Risks / Trade-offs

- [Framework migrations introduce unrelated source edits] → Review generated changes and retain only edits required for compatibility.
- [Vitest 5 changes mock clearing defaults] → Run the existing web and admin suites; adjust only a test configuration or fixture that demonstrably depends on the prior default.
- [Electron Forge 8 or Electron 45 exposes a runtime incompatibility] → Review the upstream breaking changes and run the launcher build/bundle/package checks; report any unavailable runtime boundary separately.
- [A latest direct package conflicts with Angular or Node] → Keep the newest compatible version and record the constraint rather than weakening peer validation.
- [Biome 2.6.2 reports source findings] → Fix the affected source directly and preserve the lint rules; verify the admin and web builds after moving global style rules.
- [Tailwind strict rules report arbitrary or duplicated class utilities] → Use existing or named theme utilities, or focused component styles when the value is one-off; preserve the rendered value and keep the rules strict.
- [Forge package check requires pnpm hoisting or a valid main entry] → Configure the app's linker and existing launcher bundle entry, then verify a real Forge package run.

## Migration Plan

1. Apply Angular, Material/CDK, and NgRx package migrations while their current installed versions are still available for migration detection.
2. Update the remaining compatible direct dependencies through pnpm, keeping TypeScript at `~6.0.3`, then regenerate the lockfile.
3. Convert the Forge config export to ESM for Forge 8, set its package main entry, and configure the app's pnpm linker.
4. Run web/admin builds and unit tests, lint, launcher Angular build and esbuild bundles, and Forge packaging with the hoisted layout.
5. Review the cumulative diff and revert the dependency/configuration changes together if validation reveals an unresolved incompatibility.
