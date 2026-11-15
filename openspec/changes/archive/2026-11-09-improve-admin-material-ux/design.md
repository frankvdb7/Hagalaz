## Context and decisions

The runtime boundary is the Angular admin application. Page-scoped CacheTypesStore and CacheSpritesStore use NgRx signalStore, withState, withMethods, and patchState to own request results, loading, errors, and operations. Existing HTTP services and AuthStore retain their responsibilities. Each store preserves the single pending-request guard and error handling. The page components keep signal-form models, validation, and file selection, then pass validated values to store methods.

CacheSpritesStore owns the returned image Blob and its description. The Sprites page owns the browser object URL, which an Angular effect creates from the Blob and revokes through effect cleanup on replacement or page destruction. Stores are component providers so page navigation resets state without a global cache or second state owner. Async withMethods commands preserve existing request and mutation behavior without adding a second RxJS orchestration layer.

Use the existing generated amber Material palette. Reference colors for the plan are amber primary #ffb300, primary container #604100, dark neutral #211b11, neutral text #eee1cf, and error #ffb4ab. Material generates the actual role colors. Keep the existing Semplicita Pro family, with Material title/body/label roles and sentence case.

Left aligned layout:

```text
Hagalaz admin                              Account / Sign out
[ Types | Sprites ]
Page title and short task description
[ Browse | Edit definitions ]
Primary lookup/search       Result or preview
Expandable less common operations
```

Use Material nav tabs, tabs, cards, expansion panels, buttons, form fields, progress bars, and icons. Keep layout CSS limited to spacing, sizing, grids, and overflow. Theme changes go through public Sass APIs; never style internal Material classes or put token expressions in HTML.

The first design review removed the existing ornamental forge language, introductory chips, background layers, and duplicate card borders. The distinctive amber comes from Hagalaz's existing theme rather than a second palette. Prefer readable results and safe operation grouping over dashboard decorations.

Keep unstructured cache definitions as formatted JSON because their shapes differ by type. Use named, read-only Material textareas so results support keyboard scrolling, selection, and copying. Set their monospace typography through Material form-field overrides. Do not fabricate table schemas. Preserve real empty results and make successful edits explicit. Invalid forms and duplicate pending submissions must not issue requests.

Use hCaptcha's built-in compact size to fit 320px sign-in layouts. Allow blob images in the admin image CSP because the existing preview service returns a Blob that the component displays through URL.createObjectURL. Keep the other CSP directives unchanged.

Use existing signal-form validation and Material error messages. Page methods guard validation and each store's request runner guards pending calls. A shared cache-error helper selects the first supplied ProblemDetails detail, title, or transport message, so both stores follow the same error policy. No retry mechanism is added. HTTP failures remain retryable through the same operation button.

## Verification

Build the admin project, lint src-admin, validate this change, and inspect desktop and narrow browser layouts. If live services are unavailable, use an isolated temporary preview workspace with fixture HTTP responses and the real components; report that live authentication and backend persistence were not verified.
