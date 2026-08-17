## Context

`Hagalaz.Web.App` now runs Fallow locally and in the existing frontend CI job. The first full report combines confirmed unreachable legacy files, incomplete package metadata, Sass/package-resolution issues, and static-analysis blind spots around Angular configuration, dynamic route entry points, Angular lifecycle overrides, and Inversify construction. The frontend already has one package-local Fallow configuration and one CI job; this change keeps those as the single owners.

## Goals / Non-Goals

**Goals:**

- Make the Fallow graph include the real web, admin, launcher, test-provider, and dev-proxy entry points.
- Remove only code and scaffolding confirmed unused by repository references and runtime configuration.
- Correct the broken OverlayScrollbars stylesheet path and remove obsolete direct dependencies exposed by deleted legacy code.
- Keep intentional framework/tooling behavior visible in code while documenting only narrow Fallow exceptions where static analysis cannot observe it.
- Leave the existing health findings available for a separate refactor/coverage change.

**Non-Goals:**

- No registration-flow implementation, authentication change, launcher protocol change, or route redesign.
- No replacement for Protractor or new browser-test infrastructure.
- No global suppression of Fallow rules and no broad lowering of complexity thresholds.

## Decisions

### Use explicit entry points instead of deleting referenced configuration files

Add the Angular test provider files and `proxy.conf.mjs` as Fallow entries, while retaining their existing `angular.json` references. This models the actual ownership boundary and avoids treating framework configuration as dead code.

Alternatives considered:

- Delete the files because Fallow cannot infer their references: rejected because Angular invokes them through configuration.
- Suppress all unused-file findings: rejected because it would hide genuinely dead files in the same project.

### Delete confirmed stale scaffolding and unreachable implementation

Remove the unconfigured Protractor scaffold and the unused NgRx highscores implementation, router helper, asset/type helpers, and obsolete theme index. Search results and Angular targets are the caller/ownership checks; build and test validation are the regression boundary.

Alternatives considered:

- Add missing Protractor and NgRx packages: rejected because the E2E target and NgRx implementation have no active consumer, creating dependency weight without restoring an active workflow.
- Leave dead files and suppress them: rejected because these files have no current owner or runtime entry point.

### Keep valid editor/build tooling and encode analyzer limitations narrowly

Retain `@angular/language-service` as editor tooling and `tailwindcss` as a build-time package. Add exact `ignoreDependencies`/`ignoreUnresolvedImports` entries only for these observed cases, and add `ignoreExports` for the four intentionally repeated dynamic route entry exports. Use inline, reasoned suppressions for framework/DI members that are invoked outside static call-site visibility.

Alternatives considered:

- Move Tailwind to production dependencies: rejected because it is a build-time tool and runtime production installs do not compile styles.
- Remove the language service: rejected because editor integration is a legitimate consumer not represented by source imports.
- Disable unused-dependency or unused-class-member rules globally: rejected because it would weaken the quality signal for unrelated code.

Remove the incomplete registration-form output contract rather than enabling it. The output was declared and bound by the parent, but its only emission was commented out and the parent handler performed no product action. Removing the output, listener, and no-op handler preserves the current runtime behavior while eliminating dead component API surface; implementing registration remains outside this change.

### Treat the current complexity findings as follow-up work

Do not refactor `types-manager.page.ts` or change launcher behavior merely to make this cleanup report zero findings. The remaining health findings require focused design, coverage/refactoring choices, and separate regression validation.

## Risks / Trade-offs

- [A deleted file is used through an indirect framework path] → Re-check `angular.json`, package scripts, dynamic imports, and repository references before deletion; run both frontend test targets and builds.
- [Fallow exceptions hide a future real issue] → Use exact package/import/file/member patterns with comments or config descriptions and keep all other rules active.
- [The OverlayScrollbars path correction changes CSS loading] → Use the package's documented exported stylesheet path and verify the production web build.
- [Removing Protractor documentation surprises developers] → Keep the existing Vitest test commands documented through package scripts; record E2E replacement as follow-up rather than implying coverage.

## Migration Plan

1. Update the Fallow config and package manifest, remove stale files/scripts/documentation, and correct the stylesheet import.
2. Regenerate the frontend lockfile without the removed package entries.
3. Run Fallow full and audit reports, Biome lint, frontend/admin tests, and web/admin builds.
4. If a runtime/configuration caller is found, restore the file and add an explicit entry rather than suppressing the finding.

Rollback is a revert of the cleanup change. It restores the removed files, package/script entries, stylesheet path, and Fallow configuration; no persisted data or deployed service state is involved.

## Open Questions

- The remaining four health findings need a separate decision on refactoring versus coverage-backed CRAP thresholds; this change deliberately does not decide that.
