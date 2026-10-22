## Automated checks

- Production admin build passes with no build warnings.
- Repository Biome lint passes across 245 files after the NgRx store migration.
- Admin tests pass: 3 files, 6 tests. The existing test configuration still reports that src/polyfills.ts is bundled but absent from the TypeScript program. Neither the test configuration nor that file changed.
- Fallow audit passes for the changed files with zero dead-code, complexity, or duplication findings after the NgRx store migration. No suppressions or quality configuration changes were added.
- Strict OpenSpec validation and git diff whitespace checks pass.

## Browser checks

Used the actual admin dev server for sign-in and access-denied screens. Used a temporary workspace outside the repository to render the real admin components, routes, guard, and AuthStore against fixture HTTP responses. No real cache writes were performed.

- Desktop and 320px/390px phone layouts render without a framework error overlay or page-level horizontal overflow.
- Types/Sprites navigation selects the correct Material tab. Browse/edit tabs and expansion panels expose their corresponding operations.
- Lookup displays loading and disables pending actions; results appear in labelled, read-only fields. A scalar zero remains visible.
- Negative IDs and inverted ID ranges show inline errors and disable submission.
- A fixture 404 displays an alert; retrying a valid ID succeeds.
- Empty searches explain how to adjust the query or offset; non-empty searches report match and returned-result counts.
- Saving an NPC combat level and uploading a PNG display explicit success confirmations with the target IDs.
- PNG selection displays the selected filename and enables replacement. A fixture PNG preview decodes to its expected 32×32 dimensions without CSP errors.
- Password visibility toggles; the access-denied link returns to sign-in. The compact hCaptcha widget fits at 320px.
- The fixture cache workflow reports no console warnings or errors. Screenshots were reviewed and saved outside the repository.

Live authentication, authorization-provider integration, and backend persistence were not exercised. Existing authentication and HTTP services remain the production owners.

## NgRx migration verification

- CacheTypesStore and CacheSpritesStore are provided by their respective page components and exclusively own request data, errors, and loading state. Pages retain signal forms and UI resources.
- Production build, Biome, the six existing admin tests, and Fallow audit pass.
- Fixture browser checks repeated the Types 404 alert, successful retry, pending-button disabling, and NPC edit confirmation through the stores.
- The store-backed sprite Blob renders as a decoded 32×32 PNG. Image replacement displays the success confirmation through store state.
- Navigating from Types to Sprites and back resets the definition result. Leaving and returning to Sprites resets its image preview, confirming independent component-scoped stores.
- The preview URL effect registers cleanup for replacement and page destruction. A response arriving after destruction updates only the discarded store and cannot create a browser object URL.
- The checked workflows report no browser console warnings or errors.

## Store regression tests

- Added 34 tests covering real store HTTP requests, all read/write operations, empty/scalar results, errors and retry, pending guards, independent component scopes, and preview URL replacement/destruction (including late responses).
- Admin coverage run passes: 7 files, 40 tests; JUnit XML confirms 40 tests with zero failures/errors.
- Both stores and cache-error.ts have 100% statements, branches, functions, and lines in the V8 JSON report. Overall measured admin coverage is 71.05% statements, 63.55% branches, 74.31% functions, and 72.1% lines; existing authentication and page interaction gaps remain.
- Added matching @vitest/coverage-v8 and the admin:test:coverage script without exclusions or threshold changes.
- Biome passes across 249 files. Fallow passes for 27 changed files; two inherited PostCSS dependency findings remain outside the gate.
- The existing polyfills TypeScript-program warning remains during tests.
