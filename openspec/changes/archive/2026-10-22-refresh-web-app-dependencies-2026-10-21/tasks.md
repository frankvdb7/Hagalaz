# Tasks

## 1. Upgrade packages

- [x] 1.1 Record registry versions, peer/engine constraints, and major-release changes; retain TypeScript 6.0.3 and update Biome to 2.6.2.
- [x] 1.2 Apply the Angular/Material and NgRx package migrations and verify their package groups resolve to compatible versions.
- [x] 1.3 Update the remaining compatible direct dependencies through the pinned pnpm workflow and verify the frozen lockfile install succeeds.
- [x] 1.4 Fix all Biome 2.6.2 diagnostics in source without suppressions or disabled rules.
- [x] 1.5 Enable `noTailwindArbitraryValue` and `useTailwindShorthandClasses` as errors and fix every finding without allowlists or suppressions.

## 2. Migrate the launcher configuration

- [x] 2.1 Convert `forge.config.js` to an ESM default export without changing its options, then verify Node can import it.
- [x] 2.2 Set the Electron package entry to `dist-launcher/main.js`, which is the existing launcher bundle output.

## 3. Validate and review

- [x] 3.1 Run the existing web/admin builds, unit tests, lint, launcher build/bundles, and the Forge package/config checks with the app's hoisted pnpm layout.
- [x] 3.2 Review the app-scoped cumulative diff and peer-compatible lockfile, run `git diff --check`, and confirm the acceptance criteria and non-goals.
