# Tasks

## 1. Upgrade and verify the toolchain

- [x] 1.1 Capture comparable public, admin, and launcher production build sizes on the existing graph where possible. Run supported CLI and Material migrations with pinned pnpm and align all installed Angular and Material/CDK packages on stable 22.3.x.
- [ ] 1.2 Inspect the lockfile and resolved peers, including Node, TypeScript, RxJS, NgRx, `ng-hcaptcha`, `angular-svg-icon`, and `@ngx-loading-bar`. Verify a frozen install with the hoisted linker and unchanged pnpm pin.
- [x] 1.3 Build and test both applications on the upgraded graph before feature edits. Resolve upgrade diagnostics and regressions without weakening policies.

## 2. Simplify public routing and review modernization candidates

- [ ] 2.1 Replace the six routing modules with lazy standalone route arrays. Add public router regression coverage for redirects, named outlets, titles, news subroutes, and protected profile navigation. Verify view transitions and the existing login/logout behavior.
- [ ] 2.2 Check the source and installed third-party providers for `HTTP_INTERCEPTORS`. Remove the legacy adapter if unused and verify functional auth headers and request behavior.
- [ ] 2.3 Inventory the remaining admin controls and official modernization schematics. Convert only confirmed production cases with a simpler implementation, and verify numeric/null, validation, touched, and submit behavior for any form that changes. Record the Angular Aria assessment against Material and existing keyboard controls before adding any dependency.

## 3. Recover the compact model preview

- [ ] 3.1 Add the template error boundary, failure alert, and accessible retry around the compact renderer. Verify bounded diagnostics, successful render and retry, and independent HTTP error handling with no duplicate requests.
- [ ] 3.2 Verify Item Forge draft and navigation protection with the normal renderer and with the fallback displayed. Cancel and confirm discard using the actual cache route and dialog behavior. Check that preview failure and retry issue no mutation.
- [ ] 3.3 Test route error boundary availability in the installed router and assess an actual admin route, including auth, nested outlets, and dirty/saving protection with an active route fallback. Enable only a verified scoped fallback, or record the blocker and keep the existing router configuration.

## 4. Validate and document

- [ ] 4.1 Run both production builds, both test targets, `lint:biome`, and `fallow:audit`. Collect coverage for affected logic without threshold changes and report meaningful gaps and bundle-size deltas.
- [ ] 4.2 Verify the public service worker, `launcher:build`, and Electron main/preload bundles. Verify relevant Forge packaging and launcher startup when affected, and report any environment blockers.
- [ ] 4.3 Verify desktop and phone-width public navigation and auth forms, admin auth, Cache Studio editing and preview recovery, focus, keyboard use, and Material overlays. Record any checks that require user browser verification separately from automated results.
- [ ] 4.4 Document adoption, retained APIs, and skipped candidates with code locations and rationale. Record the template preview caveat and rollback. Update frontend guidance only if a compatibility rule changes.
