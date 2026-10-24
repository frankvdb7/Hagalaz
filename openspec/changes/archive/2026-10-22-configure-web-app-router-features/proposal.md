# Proposal

## Why

The public and admin Angular applications should use the requested router platform navigation feature and automatic cleanup for route-created injectors.

## What Changes

- Configure `withExperimentalPlatformNavigation()` and `withAutoCleanupInjectors()` in both app router providers.
- Keep the existing route definitions and other router options unchanged.

### Non-goals

- Do not change routes, guards, navigation UI, or lazy-loading boundaries.
- Do not use the deprecated `withExperimentalAutoCleanupInjectors()` alias.
- Do not change router behavior outside these provider options.

### Acceptance Criteria

- Both public and admin app bootstraps provide the two requested router features.
- The injector cleanup provider uses the non-deprecated `withAutoCleanupInjectors()` API.
- Both Angular application builds succeed and the OpenSpec change validates.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `web-app-toolchain`: specify the router features used by both application bootstraps.

## Impact

- `Hagalaz.Web.App/src/main.ts`
- `Hagalaz.Web.App/src-admin/main.ts`
- `openspec/specs/web-app-toolchain/spec.md`
