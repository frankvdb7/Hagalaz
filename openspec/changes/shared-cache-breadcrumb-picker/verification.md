# Verification

- Admin production build passed.
- Admin coverage run passed all 188 tests across 14 files. Lines 95.74%, statements 94.29%, branches 92.87%, functions 89.12%.
- Picker tests cover all 13 type routes with ID zero, NPC name search, unsupported name-search prevention, debounce, failure/retry, keyboard dismissal and focus restoration.
- Generic route tests verify each type's breadcrumb and cross-type replacement/cleanup. Existing item mapping, supported editors and Sprite Studio tests continue to pass.
- Biome lint passed with no findings.
- Fallow audit returned warn, with no introduced dead-code, complexity or styling findings. Existing service-member and service-wiring clone findings remain. No suppressions or weakened rules were added.
- Strict OpenSpec validation passed all six active changes. Git diff whitespace validation passed.

## Browser

Bundled Playwright with Microsoft Edge checked all 13 cache detail routes against the real admin dev server using temporary auth/API fixtures. Every picker navigated to ID zero within its own type. NPC autocomplete used its existing search endpoint. Sprite text input requested a valid ID without a name search. The phone popup remained inside a 390px viewport; no browser runtime errors were observed.

Desktop NPC and phone Sprite screenshots are stored outside source in the local temporary `hagalaz-shared-picker-qa` folder. Live backend authentication/cache data were not tested.

## Self-review

No stores, HTTP services, backend endpoints or mutation paths were added. The picker reads the selected definition and optional name-search operations from the existing page store. One template registration directive replaces item-specific registration code. A cross-type test exposed early rendering before required inputs were assigned; registration now uses Angular's afterNextRender lifecycle. Template labels and hints use derived signals to keep control flow readable.

The existing polyfills test-compilation warning remains. No dependency changes were made.
