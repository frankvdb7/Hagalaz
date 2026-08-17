## 1. Make the Fallow graph accurate

- [x] 1.1 Update `.fallowrc.json` with the actual web/admin/launcher/configuration entry points and exact, reasoned exceptions for Tailwind imports, editor/build dependencies, dynamic route exports, and framework/DI members.
- [x] 1.2 Correct the OverlayScrollbars stylesheet import and remove `@angular/platform-server`; regenerate `pnpm-lock.yaml` while retaining legitimate `@angular/language-service` and Tailwind tooling.

## 2. Remove confirmed dead and obsolete frontend code

- [x] 2.1 Delete the unreachable legacy NgRx highscores actions/reducer, router helper, asset/type helpers, and obsolete theme index after rechecking repository and Angular configuration references.
- [x] 2.2 Remove the unconfigured Protractor E2E scaffold, `e2e` package script, and stale README instructions without adding a replacement E2E framework.
- [x] 2.3 Remove unused logger methods, make internal-only state/result types non-exported, remove the unused registration-form input and incomplete output/listener contract, and add narrowly scoped comments for the remaining Angular lifecycle/DI boundaries that Fallow cannot observe.

## 3. Verify the cleanup and preserve the existing runtime boundary

- [x] 3.1 Run the full Fallow report and changed-file audit; confirm the targeted dead-code, dependency, unresolved-import, and duplicate-export findings are resolved or explicitly bounded, and record the remaining health findings as follow-up.
- [x] 3.2 Run `pnpm run lint:biome`, `pnpm run test`, `pnpm run admin:test`, `pnpm run build`, and `pnpm run admin:build` from `Hagalaz.Web.App`.
- [x] 3.3 Run `git diff --check`, inspect the final diff for unrelated changes, and verify the frontend CI job still invokes the package-local Fallow audit with full history.
