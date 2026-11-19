# Design

## Context

Both Angular applications use the same package graph and each has an application build and Vitest target. The public project also builds for Electron and registers a service worker. Version 22.3.0 is published for the Angular framework, CLI/build, Material, and CDK. npm metadata for NgRx Signals, `ng-hcaptcha`, `angular-svg-icon`, and `@ngx-loading-bar` allows this Angular release. The installed Node 24.16.0 and declared TypeScript 6.0.x fit the target. From the frontend directory, Corepack selects the declared pnpm 12.21.0.

## Goals and non-goals

Preserve both application entry points and the launcher while upgrading their toolchain, simplifying public route configuration, and isolating one real preview renderer. Scope excludes replacing NgRx, changing server endpoints, introducing SSR, and adopting new primitives without a demonstrated benefit.

## Decisions

### Upgrade the package graph before feature edits

Use Angular CLI's update workflow for core/CLI and Material/CDK, with all installed Angular packages aligned on stable 22.3.x. Keep pnpm 12.21.0, the hoisted linker, TypeScript 6.0.x, and unrelated dependency versions. Inspect migration and lockfile diffs, verify the resolved peers and frozen install, then run both applications' builds and tests before feature edits. Fix real diagnostics at the source. Record production bundle sizes before and after when measurement is available.

### Preserve the public route tree

Each of `news`, `highscores`, `play`, `login`, `profile`, and `register` has one pass-through NgModule. Export standalone route arrays and keep the existing `loadChildren`/`loadComponent` boundaries. This preserves the `ActivatedRoute` hierarchy, including the highscores route data, news subroutes, and the profile guard and nested redirect. Header and footer already load standalone routes. The source currently has no `HTTP_INTERCEPTORS` registration. Inspect third-party Angular providers in the installed graph before removing `withInterceptorsFromDi()`. The functional auth interceptor retains ownership of auth headers.

### Keep renderer recovery inside the preview

Use Angular's developer-preview `@boundary`/`@error` inside the compact view in `cache-model-preview.component.html`, around `hagalaz-model-viewer` and its bindings. Item Forge reaches this view from its model inspector. Keep model and texture resources, their request retries, and the Item Forge draft outside the boundary. The Angular block handles view failure and user-triggered `$reset()`. Render a static alert and a native Material button labelled “Retry preview.” Verify focus after the renderer is restored or the retry fails. Use Angular's view-error hook if reporting is needed, with a bounded diagnostic per failed render attempt and no raw error in the UI. Avoid polling, automatic retries, or a second telemetry service. Roll back this preview API by removing the block and its fallback and restoring the direct viewer; the editor, requests, and shared viewer need no changes.

### Gate router error boundary adoption on draft ownership

The published 22.3.0 router typings expose `withErrorBoundaries()`. The cache guards call the route-provided `CachePendingChangesService` and ignore the component argument. That service tracks a component-owned draft and unregisters the editor on destruction, so a route fallback that replaces an editor may lose protection even if `canDeactivate` runs. Assess this with the real admin route configuration in tests. Only enable a single route fallback if auth, nested outlets, and dirty/saving protection survive, including when the fallback is active. Keep the template boundary as the default recovery design. Guard and resolver errors belong to the navigation pipeline.

### Evaluate remaining forms and accessible controls in place

Review Model Lookup's numeric validation, nullable input and reset on route ID changes as the first Signal Forms candidate. The object shape input is a local selector, and model property controls emit per-axis values. Preserve Material validation, touched state, and submit behavior where a conversion would add code or duplicate state. Existing public and admin auth forms already use Signal Forms. The definition picker uses Material autocomplete, the map scene panel uses Material tree, and Sprite Studio combines a CDK virtual viewport with tested listbox navigation. Adopt Angular Aria only if a specific candidate can show improved keyboard or screen-reader behavior. Apply schematics only to confirmed production patterns.

## Risks and trade-offs

The preview block handles template and change-detection failures. Expected request failures and renderer handling of unavailable WebGL keep their existing feedback. Regression tests should throw from the renderer view, recover via the retry button, and prove that resources and the editor remain mounted. Include both cancellation and discard when leaving a dirty editor. Verify stable package support using the installed typings, npm metadata, and CLI migrations. The issue's Angular GitHub release-tag link returned 404 during planning.

See the [Angular template error boundary guide](https://angular.dev/guide/templates/error-boundaries), the [router error boundary guide](https://next.angular.dev/guide/routing/error-boundaries), and the [Material/CDK releases](https://github.com/angular/components/releases).

## Toolchain verification

All direct Angular packages resolve to stable 22.3.0. A clean frozen install with pinned pnpm 12.21.0 passed, including with `--strict-peer-dependencies`. Strict dependency resolution without the frozen flag failed on the optional `@oxc-parser/binding-wasm32-wasi@0.158.0` dependency of `@angular/build@22.3.0`. The binding pins `@emnapi/core` and `@emnapi/runtime` to `2.0.0-alpha.6`, outside the ranges declared by its `@napi-rs/wasm-runtime@1.2.7` dependency. The registry had one stable Angular Build 22.3 release when checked. The peer check remains open and the worktree has no peer overrides or release-age exceptions. [Angular Build metadata](https://registry.npmjs.org/@angular/build/22.3.0) · [Oxc binding metadata](https://registry.npmjs.org/@oxc-parser/binding-wasm32-wasi/0.158.0) · [WASM runtime metadata](https://registry.npmjs.org/@napi-rs/wasm-runtime/1.2.7)

Both production builds and test targets passed before feature edits, with 31 public and 670 admin tests. The test builder's `src/polyfills.ts` warning also appeared on the 22.2 baseline. The launcher build and Electron main/preload bundles passed. Initial bundle sizes in kB on 22.2 and 22.3 were 475.10 and 483.54 for public, 675.97 and 680.45 for admin, and 475.11 and 483.55 for launcher. Estimated transfer sizes fell from 124.01 to 121.16, 161.05 to 156.30, and 124.01 to 121.19 kB respectively.

The requested refresh of the remaining frontend dependencies updated 16 direct entries. TypeScript stays at 6.0.3 to satisfy Angular's supported range. Vitest 5.1.1 uses Angular Build's Vite 8.3.8 and pnpm reports an advisory about the transitive peer. The frontend invokes test targets through Angular CLI. Frozen install, both production builds, all 701 standard tests, Biome, Fallow, launcher builds and bundles, and Windows x64 Forge packaging passed. The map scene texture test timed out with coverage on both the original and upgraded graphs. After the test was changed to drain fake timers directly, all 670 admin tests passed with coverage and reported 70.59% line coverage.
