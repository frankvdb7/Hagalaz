## Why

The admin overview needs to match the supplied visual reference while retaining its working navigation and honest disconnected states.

## What Changes

Refine the overview and its shared shell layout, density, visual hierarchy and artwork using existing Material components and theme tokens.

## Capabilities

### New Capabilities
- `admin-overview-reference`: Reference-aligned overview presentation.

### Modified Capabilities
None.

## Impact

Admin overview, shell styling and artwork. Reuse existing router, authentication, Material theme and cache archive store.

## Acceptance criteria

- A1: Desktop overview and navigation follow the supplied reference; layouts remain usable without horizontal page overflow on narrow screens.
- A2: Navigation and archive count actions retain their existing behavior; disconnected metrics remain clearly identified and no data is invented.
- A3: Admin build, scoped lint, strict validation and browser checks pass, with access limitations reported.

## Non-goals and stop conditions

No new backend, telemetry, authentication, routes, packages, or speculative data. Keep existing user changes intact.
