## Why
Cache definitions need independent state owners and dedicated customizable detail pages instead of one multi-type inspector.

## What changes
Provide one NgRx signal store and dedicated lazy detail component for each of the 13 supported types. Use Angular HTTP resources for signal-driven reads and NgRx rxMethod/tapResponse for supported writes. Keep existing HTTP contracts, guards, Material design and sprite image tools.

## Non-goals
No backend changes, invented edits, new dependencies, global type stores, or changes to authentication/public app.

## Acceptance criteria
- A1: Each type owns an independent SignalStore; signal resources own read loading/error/value and rxMethod owns writes. No async/await store request wrappers.
- A2: Each type has a dedicated guarded /cache/types/<type>/<id> page, reachable from lists and numeric lookup.
- A3: Four supported editors retain validation, confirmation, server feedback and reload; unsupported fields stay read only.
- A4: Existing list pagination, archive counts and sprite image workflows work.
- A5: Store isolation/cancellation, all detail routes, edits and list navigation have regression tests; build/lint/coverage/browser checks pass.
