# Verification

- Admin production build: pass, no build warnings.
- Full admin tests: 107 pass / 11 files. All 13 dedicated detail routes, independent type stores, lookup cancellation/reload/error handling, four mutations, validation/confirmation, paginator requests and sprite lifecycle covered.
- Admin coverage: statements 90.78%, branches 90.71%, functions 88.64%, lines 92.26%. Cache stores: statements/functions/lines 100%, branches 97.95%. Type detail components/editors: functions/lines 100%, branches 96.98%. Shared detail scaffold TypeScript: statements/functions/lines 100%, branches 92.68%. Remaining shared-store branch guards non-object mutation data; no synthetic backend contract violation was added to force coverage.
- Biome lint: pass (301 files). Whitespace: pass. OpenSpec strict validation: pass.
- Fallow audit: fail on four introduced unused-member reports and four inherited reports, all in CacheTypesService. Duplication findings resolved. `fallow inspect --file .../cache-types.service.ts` confirms production reachability/importers; source and tests demonstrate resource factory callers. No weakening or suppression added.
- Existing test warning: public src/polyfills.ts is bundled but outside admin test TS compilation.

## Runtime evidence
Fixture preview imports real admin routes/components/stores with HTTP fixtures outside the repository. Numeric NPC lookup opens /cache/types/npcs/7, hydrates combat level 2, confirms a change to 42, displays server confirmation and updates the local resource value. Back link returns to NPCs selected. Dedicated sprite metadata page links to /cache/sprites; PNG frame preview still loads. NPC detail fits 390px with document scrollWidth 390. Desktop/phone screenshots saved outside source under C:/Users/fmavd/AppData/Local/Temp/hagalaz-portal-tests/. Browser tests are fixture-backed, not proof of real backend persistence.

## Requirement evidence
| Requirement | Evidence |
| --- | --- |
| Independent stores for every kind | loads only the requested items definition using signal resources (parameterized over all 13), keeps item and NPC requests and results independent |
| Reactive cancellation and route lifetime | cancels stale lookups and reloads an unchanged ID; destroys pending resources with their page injector |
| Dedicated detail page for every kind | loads the dedicated guarded items page and its own definition (parameterized over all 13); reloads the same route ID and follows ID parameter changes |
| Validated supported edits | validates and confirms the dedicated npcs editor before saving; blocks invalid npcs editor values without a request or dialog (both parameterized across all four editors) |
| List paging and navigation | pages ID ranges and links rows to their dedicated detail route; paginates the submitted query, changes page size and resets category state |
| Existing binary image lifecycle | keeps the previous preview when the next image request fails; revokes the old preview on replacement and the active preview on destruction |

## Backend limits
No backend changes required. Mutations remain limited to existing NPC combat level, object name, varp-bit/config fields and sprite image endpoints. Other definition data stays read only; no unsupported search or edit endpoints were invented.

## Automatic loading follow-up
Admin tests: 107 passing. Coverage: statements 91.01%, branches 90.89%, functions 88.72%, lines 92.28%; types-manager TypeScript lines/functions 100%. Production build and scoped Biome pass. Fixture browser confirms category selection loads without search submission and retains paginator size. Existing polyfills warning and eight known Fallow unused-member false positives remain.

## Type service split
All 13 type stores use dedicated services. Existing sprite service reused. Monolithic CacheTypesService removed and no remaining callers found. Pure cache-resource functions share resource construction only; fixed URLs and writes remain in each service. Admin suite: 118 passing across 11 files. Coverage statements 91.37%, branches 91.64%, functions 89.66%, lines 92.72%. Build and Biome pass. Existing polyfills warning persists. Fallow remains failing: 32 introduced and four inherited unused-member reports on exercised services, plus three duplicate groups of service method signatures/delegation. Short service wiring intentionally retained to keep type boundaries explicit; no inheritance, suppression or rule changes. Service HTTP request contracts exercised through production store integration tests. No live backend persistence verification for this refactor.

Explicit service wiring follow-up: each store now directly injects and calls its dedicated service resource methods. Shared features only invoke resource callbacks. Existing 118 admin tests and production build pass. Biome passes. Fallow still reports the same service unused-member false positives and short delegation duplicates; direct call sites and HTTP integration tests demonstrate runtime use.

## Sprite detail image integration
Reuses existing GET types/sprites/{id}/image?frame and multipart POST types/sprites/{id}/image; backend source TypeEndpoints and TypeMutationEndpoints inspected. Embedded tools hide global lookup/search, pin ID through Signal Forms readonly schema, auto-preview zero, and refresh preview/metadata after confirmation. Standalone tools preserved.
122 tests pass across 11 files. Overall coverage: statements 93.35%, branches 92.36%, functions 90.71%, lines 93.78%. Sprite tools TypeScript: lines/functions 100%, branches 94.11%. Sprite detail lines/functions/branches 100%. Remaining sprite branches concern optional-file/standalone paths. Existing URL cleanup/cancellation and confirmation-cancel tests remain passing.
Production build and lint pass. Browser fixture renders PNG and replacement controls at desktop and 390px phone width, with no horizontal overflow or console warnings/errors. Images saved outside repository in Temp/hagalaz-portal-tests/sprite-detail-desktop.png and sprite-detail-phone.png. Fixture metadata matches backend sprite response shape; fixture images are illustrative. Live API/persistence not verified because the API is offline. No fake responses added to production. Existing polyfills warning and Fallow 32 new plus four inherited unused-member reports and three short service wiring duplicate groups remain unchanged.
