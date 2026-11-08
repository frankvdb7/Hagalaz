# Tasks

## 1. Quantity rendering

- [x] 1.1 Port the client stack variant rule (World.method6168) and quantity label rule and formatting (ItemDefinition.method6279, Class_ta_Sub3.method5686). Verify: unit tests for variants, modes, thresholds and colours.
- [x] 1.2 Draw labels with the `p11_full` font metrics and glyphs like the software client (Class263.renderText, Class263_Sub3), including colour tags and the shadow. Verify: unit tests with a synthetic font and the pinned `p11_full` metrics.
- [x] 1.3 Add `quantity` and `quantityMode` to the endpoint; inner note, lend and bought items use the client quantities and no label. Verify: endpoint tests for variants, every mode, abbreviations, validation and the missing-font 422.

## 2. Verification

- [x] 2.1 Run the cache library and cache service suites. Verify: commands pass; results recorded below.

## Notes

- Verified on the real cache: coins render the client pile variants at 1,000 and above, labels show `1000` in yellow, `250K` in white and `25M` in green, and a noted whip shows its quantity.
- Default label rule changed on request: without `quantityMode`, only stackable items with a quantity above zero are labelled; the client modes stay available explicitly.
- 2.1, 2026-11-07: `Hagalaz.Cache.Tests` 484 passed and `Hagalaz.Services.Cache.Tests` 201 passed; no new warnings in the changed files.
