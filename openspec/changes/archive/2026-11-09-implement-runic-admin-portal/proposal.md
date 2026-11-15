## Why

The existing cache tools need a shared, branded administration shell matching the supplied Runic Material references.

## What changes

Add a guarded responsive portal with overview, cache studio, and honest integration pages for the remaining navigation. Preserve cache routes, stores, APIs, authentication, and sprite lifecycle. Apply navy Material surfaces, canonical amber branding, and purposeful cyan/emerald/violet accents.

## Non-goals

No backend domains, fabricated metrics, new dependencies, generic JSON editor, or changes to the public application theme.

## Acceptance criteria

- A1: One shared Material drawer/toolbar shell with eight destinations, accessible mobile navigation and existing identity/sign-out.
- A2: Overview has branded sigil hero and useful links; missing metrics and integrations are explicitly unavailable.
- A3: Existing cache URLs, supported kinds, reads, searches, mutations and sprite workflows remain usable with structured controls and raw inspection.
- A4: Reference composition and accents work at desktop and phone sizes without page overflow; real Material controls retain keyboard focus.
- A5: Focused route, guard, cache and lifecycle tests, admin build, coverage, lint, audit and browser comparison complete with limitations recorded.

## Stop conditions

Record backend integration gaps instead of expanding this change into new server domains. Reuse existing stores and authentication rather than adding parallel owners.

## Capabilities

### New capabilities
- `admin-portal`: Shared branded navigation and supported/unavailable feature entry points.

## Impact

Only the admin frontend, its design guidance, tests, and this change record. Existing HTTP services remain authoritative.
