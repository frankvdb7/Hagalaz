# Proposal

## Why

Upgrade the shared public, admin, and Electron frontend toolchain to stable Angular and Material 22.3. Use the upgrade to simplify public lazy routing and let Cache Studio users recover from a failed model render without leaving their editor.

## What changes

- Upgrade Angular framework and tooling, Material, and CDK together using the supported migrations and pinned pnpm. Preserve strict diagnostics, the hoisted linker, service-worker behavior, and Electron packaging.
- Replace the six pass-through public routing NgModules with lazy standalone routes, preserving titles, redirects, guards, nested routes, and named outlets. Verify the public legacy HTTP interceptor adapter is unused before removing it.
- Isolate the compact Cache Studio model renderer. Give users a keyboard-accessible retry while the editor and preview's data requests stay mounted.
- Review local admin forms, modernization schematics, Angular Aria, and router error boundaries against existing code. Adopt changes only where they simplify the implementation or deliver a verified improvement.

## Capabilities

### New capabilities

None.

### Modified capabilities

- `cache-model-viewing` gains recovery from a compact preview render failure while preserving pending editor changes.

## Impact

Both Angular applications, the Electron launcher, and the shared package manifest and lockfile are affected. Reuse the existing router, Material components, preview resources, and cache pending-changes service. Backend contracts, authorization, state ownership, and the existing test infrastructure stay in scope as regression checks. SSR, broad form and Material rewrites, and additional dependencies without a verified use are out of scope.

## Acceptance and stop conditions

Require a stable, compatible 22.3.x graph, a frozen install, both apps' builds and tests, lint, Fallow, and launcher validation. Exercise route and guard behavior, rendering failure and recovery, existing HTTP errors, and dirty editor navigation. Collect coverage, compare build sizes, and record browser smoke results or the specific checks that remain unverified. Stop to resolve any incompatible peer or required migration that cannot be addressed within the existing toolchain and scope. Route-level fallback requires separate evidence that it can preserve authorization, nested outlets, and draft protection.
