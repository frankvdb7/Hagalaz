## Why

The admin cache tools mix Material controls with bespoke decoration and show all read and write operations at once. Staff need a clearer way to inspect cache data and deliberately edit definitions.

## What Changes

Use Angular Material for the admin shell, sign-in, access-denied page, and cache tools. Separate browsing from editing, label operations clearly, and show validation, loading, errors, and successful results where staff can find them.

## Capabilities

### New Capabilities
- `admin-material-ux`: Consistent, accessible Material administration workflows.

### Modified Capabilities
None.

## Impact

Only `Hagalaz.Web.App/src-admin` and this change record. Reuse existing Angular Material packages, signal forms, services, and AuthStore.

## Acceptance criteria

- A1: All admin screens use Material components and the existing theme, with no decorative gradients, internal MDC selectors, or CSS variables in HTML.
- A2: Navigation identifies the active page; browse and edit workflows remain available and readable on desktop and narrow screens.
- A3: Forms explain required values, disable invalid or pending submissions, and expose loading, error, empty, and result states accessibly.
- A4: Production build and Biome pass; browser checks cover navigation, read/write feedback with isolated fixtures, and narrow layouts.
- A5: Types and Sprites each use a page-scoped NgRx signal store as the sole owner of request state and operations. Form state and preview URL resources remain in the pages.

## Non-goals and stop conditions

No API, authorization, dependency, public app, or launcher changes. No new cache operations or generic design framework. Record any backend defect separately.
