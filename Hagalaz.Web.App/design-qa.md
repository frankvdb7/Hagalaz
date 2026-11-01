# Admin login design QA

## Source and implementation

- Source visual: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-5992a94a-9e03-4107-ae7c-6054eac99ec7.png` (1648×954 px).
- Implementation capture: Codex in-app browser screenshot output for `http://localhost:57145/login`; captured at 1648×954 CSS px and DPR 1. The browser API emitted the screenshot but did not provide a local screenshot file path.
- Responsive check: 390×844 CSS px at DPR 1.
- State: email empty and touched with its validation message; password empty and untouched; captcha incomplete; no focused control; sign-in disabled.

## Comparison evidence

- Separate inspection: the source and implementation were each inspected at a 1648×954 px viewport and matching form state. The implementation card measures 468×628 px at (590, 209); its centered brand, card proportions, field positions, and spacing appeared consistent with the reference, but a direct side-by-side comparison was not completed.
- Focused regions: logo lockup, card edges, stacked email/password labels, field outlines, captcha, disabled sign-in action, and access note were inspected. At 390 px, the card remains within the viewport and the document has no horizontal overflow.
- Interactions: the email error appears after blur, the password visibility button toggles between `password` and `text`, and sign-in remains disabled while required input and captcha state are invalid. No credentials were submitted. The browser console had no errors or warnings.

## Findings

- No obvious P0, P1, or P2 layout issue was found during separate inspection; confidence is limited because the direct comparison remains incomplete.
- Expected local differences: the development hCaptcha test key displays its provider warning inside the widget. The reference shows an enabled amber sign-in button, while the real form correctly disables submission until inputs and captcha are valid. Authentication configuration and behavior were left unchanged.

## Shared Material theme follow-up

- After the shared theme update, the login rendered its outlined card with the Material low surface-container color (`rgb(22, 31, 45)`) and 16 px corners. Both form fields remained 64 px high, and the primary/on-primary Material variables resolved.
- With the email, password, and captcha empty, the sign-in action remained disabled. The 1280×720 in-app browser view had no horizontal overflow, and its console had no errors or warnings.

## Comparison history

1. Initial desktop inspection found the captcha in compact mode and the backdrop drawn with CSS. Replaced the backdrop with a generated image asset and changed hCaptcha to its wide normal mode.
2. The next inspection found floating labels and shorter fields than the reference. Added accessible stacked labels, 64 px Material fields, dynamic error space, and reference-aligned row spacing.
3. Desktop and 390 px inspections confirmed the target geometry, error state, password toggle, disabled action, and lack of horizontal overflow.
4. Centralized card surfaces and filled action colors through Material system roles; rechecked resolved tokens, card shape, field height, disabled state, and console output.

The browser security policy rejected opening the temporary `file://` page needed to place the source and implementation screenshots side by side. No alternate browser route was attempted. The captures were inspected separately, but the required combined comparison input could not be produced.

**final result: blocked**

Blocker: the in-app browser's URL policy rejected the local-file comparison page, so the required side-by-side source-to-render check remains incomplete.

## Overview reference QA

- Source: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-1ba810c2-8d65-4398-b2fd-c1f84562b2e3.png` (1644×957).
- Implementation: `C:/Users/fmavd/AppData/Local/Temp/hagalaz-overview-final5.png` (1644×957); comparison: `C:/Users/fmavd/AppData/Local/Temp/hagalaz-overview-comparison5.png`; focused comparison: `C:/Users/fmavd/AppData/Local/Temp/hagalaz-overview-focus.png`. Viewport 1644×957 CSS px, density 1. Fixture bypasses the guard and has no user identity or backend connection.
- Layout: the first comparison showed oversize hero and metric panels, with lower panels below the fold. Reduced spacing and control heights; the final comparison shows the same overall section order and lower panels within the viewport. A 390px view had no page-level horizontal overflow; its drawer and Worlds navigation worked.
- Typography and copy: existing Semplicita Pro and Material roles were retained. Most source copy and unavailable-state text match. The fixture account identity differs from the reference.
- Colors: shared navy and amber Material roles are used; the hero is flatter and lighter than the source.
- Images: existing brand logo and generated orbit image replace the old CSS-drawn sigil. The orbit remains less crisp and less detailed than the source.
- Interactions: mobile drawer and Worlds navigation worked; archive count is covered by existing tests but was not exercised against a live backend. No console errors or warnings were observed in the fixture.
- Remaining P2 visual differences: orbit detail and hero background differ from the source; the unavailable activity panel does not include the reference grid or time selector. A time selector is intentionally omitted because no telemetry exists and it would be nonfunctional.
- Follow-up: refine orbit art and hero treatment, and verify in an authenticated session. Preserve the truthful disconnected state.

final result: blocked

## Cache Studio reference QA

- Source: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-a78f8890-d883-444f-adb5-c254f9bb115d.png`; implementation: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-final2.png`. Both are 1649×954 pixels at a 1649×954 CSS viewport and density 1. Full comparison: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-comparison-final2.png`; focused comparison: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-focus-final2.png`.
- State differs: the isolated fixture bypasses authentication, returns sample rows and archive sizes, and returns 404 for item image requests. The reference shows real item artwork and a named account. The fixture repeats its sample rows on later pages and is not evidence of live data correctness.
- Layout and spacing: initial preview showed a narrower workspace, oversized search and wide ID columns. Widened the cache workspace, separated tabs and search, adjusted Material density, and resized columns and item thumbnails. The final focused comparison shows the same broad panel order and ten rows with pagination in the viewport. At 390px there was no page-level horizontal overflow.
- Typography and colors: the implementation uses bundled Semplicita Pro and existing Material navy/amber roles. Text and cards are readable; the source has smaller text and darker surfaces. The shared login backdrop is intentionally retained per the user's earlier request.
- Copy and controls: real category tabs, search, pagination and detail links are preserved. The source's descriptions, sorting, filters, count badge and view modes are not present because their behavior or data is not provided by the current contracts; no speculative data or controls were added.
- Images: the source contains actual item images. The fixture did not supply PNGs, so its visible unavailable indicators cannot establish image scale or fidelity against real results. The app continues to use its existing thumbnail endpoint and cleanup.
- Interactions: category selection, name search and pagination navigation were checked in the isolated fixture. The mobile layout was inspected. Fixture image 404 errors are expected; live authenticated data and image rendering were not checked.
- Remaining P2: real thumbnail rendering and the populated live table need a reference-sized comparison. Do not infer that image fidelity passed from the fixture.
- Follow-up: inspect actual cache thumbnails and result states against the reference using a connected admin session, then update this QA result.

final result: blocked

## Cache Studio live browser QA follow-up

- Source: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-a78f8890-d883-444f-adb5-c254f9bb115d.png`. Live capture: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-live-final.png`. Both 1649×954 pixels at a 1649×954 CSS viewport. Full comparison: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-live-final-comparison.png`; focused comparison: `C:/Users/fmavd/AppData/Local/Temp/cache-studio-live-final-focus.png`.
- State: authenticated Items page, first ten definitions and 25,478 ID slots. Real item thumbnails loaded. The viewport override was reset after capture.
- Comparison history: the fixture pass could not verify thumbnails. The first live comparison showed all thumbnails but clipped the paginator at the bottom. Reduced the item thumbnail box from 40px to 36px and aligned it within the row; the final comparison shows all ten rows and the paginator at the reference viewport.
- Typography and layout: Semplicita Pro, panel order, labels, category navigation, search and table align with the intended admin design. The app retains 13 categories and scrollable navigation; the reference shows fewer categories. Narrow layout and category, search and pagination interactions were checked in the prior fixture pass.
- Colors and imagery: the shared navy/amber theme and user-requested login backdrop remain. The live sprites correspond to the pictured item identities. The source uses a darker canvas; this is an intentional theme difference.
- Copy and controls: names and ID range match. Reference descriptions, filters, sorting, view modes and an extra actions column are omitted because the current API or workflow does not support them. Existing names link directly to the detail page. This is an intentional scope difference rather than a live-data claim.
- Runtime: no browser errors were returned by the browser log. Five Angular NG0912 component-ID collision warnings were observed in the development session; they are not attributed to this styling change. Focused admin tests: 20 passed; build and lint passed. The test build reports the existing `src/polyfills.ts` TypeScript compilation warning.
- Remaining P3: the Material paginator alignment and text sizing are not identical to the mockup. No actionable P0/P1/P2 visual issue remains within the supported scope. Live interaction and mutation flows were not exercised.

final result: passed

---

# Item Forge reference QA

final result: blocked

Source: user-selected codex-clipboard-07acb157-b7a8-4416-9c82-b0e9410197d3.png, 1651x953 desktop reference.
Implementation: existing Angular admin Item Forge, /cache/types/items/0.

The user previously instructed that browser checks be skipped. That instruction remains in effect, so no rendered screenshot, viewport/state comparison or visual design pass was performed. Automated tests and production builds do not substitute for visual QA. No pixel-fidelity or browser-interaction pass is claimed.

Implemented structure: source-backed item portrait/header/chips, save/duplicate and navigation actions, icon tabs, two-column editor, compact zoomable inventory image, expandable existing 3D inspector, technical detail copy buttons and quick actions. Existing Material Symbols, supplied Hagalaz logo and Semplicita Pro fonts are reused. Small viewports stack the layout.

Intentional behavior differences: noted remains derived; descriptions are unavailable in this cache format and are labeled accordingly. Save sends the full ItemType, per the user's explicit correction. Cache writes are exercised only in isolated test stores. Existing application shell remains unchanged.

Follow-up visual gate: capture this route at 1651x953 with item0 and compare it alongside the supplied source, then check a narrow viewport and primary interaction states. No visual findings are invented from source code alone.


---

# Unified Material admin and Item Forge QA, 2026-10-31

This is the latest QA result. The current request explicitly calls for existing Material components and unified styles across the admin app. It supersedes pixel-exact custom-control styling from the reference. Earlier QA sections above remain historical records.

## Source, state and normalization

- Source: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-56eca79f-b8e4-4628-a64a-f20728307ac4.png`, 1651 x 953 pixels.
- Implementation: `http://localhost:4318/cache/types/items/0`, actual Angular admin components compiled with an isolated fixture entry point under ignored `node_modules/.cache/forge-preview`. Production bootstrap, authorization and API configuration are unchanged.
- Desktop capture: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/admin-material-desktop.png`, 1651 x 953 pixels at 1651 x 953 CSS px, density 1. No density resampling.
- State: item 0, Dwarf remains, value 1, members only, Overview selected, no unsaved draft, inspector collapsed. Save is correctly disabled until edited. Description remains honestly unavailable.
- Phone: 390 x 844 CSS px, density 1. `admin-material-phone.png` and `admin-material-phone-fields.png` in the same evidence directory. Observed document width 390 px, no horizontal page overflow.

## Visual comparison

Opened the source and rendered capture together in `admin-material-comparison.png`, 3302 x 953 pixels, source left and implementation right. Also opened `admin-material-fields-comparison.png`, with actual source and implementation form crops stacked without resampling, to inspect labels, field outlines, typography, toggles and helper text.

- Fonts and typography: existing Semplicita Pro and Material Symbols load correctly. Standard Material label and body roles now own controls. No handwritten icon replacements or additional fonts.
- Spacing and layout rhythm: identity, actions, tabs, two-column form and inspector retain the reference hierarchy. Standard 48px fields, 44px actions and accessible icon controls intentionally replace the reference's smaller custom controls. Phone fields stack and remain readable.
- Colors and tokens: shared navy surfaces, amber actions, outline roles and semantic error/success colors. All admin Material overrides live in `src-admin/portal-theme.scss`; no page-local Material overrides remain.
- Image quality: production reuses the existing item sprite renderer, logo and backdrop. Fixture sprite is an 88px source crop used only in the ignored preview harness. Its baked navy background and enlarged appearance in Sprite Studio are fixture limitations; production uses API images.
- Copy and content: actual field values and character counts are retained, including the correct 13/50 name length. Description is readonly and unavailable, noted status remains derived, and no archive count is fabricated in production.

## Intentional differences from the source

Material outlines, chips, toggles, slider, card corners and expansion panel follow the user's explicit correction. The existing shared Cache Studio heading, navigation drawer width and backdrop remain. These increase vertical content compared with the mock. The complete technical metadata and quick actions remain reachable through normal scrolling. The source's save dropdown and unsupported description editing were not invented.

## Additional admin surfaces

Captured and inspected `admin-material-overview.png`, `admin-material-cache-studio.png`, `admin-material-sprite-studio.png` and `admin-material-login.png` in the same evidence directory. Shared field and action sizing, card surfaces, tab treatment and typography remain coherent. Sprite preview geometry/checkerboard remains specialized. Login now uses native Material labels and no positioned substitute labels.

## Interaction evidence

- Edited the name, opened the existing confirmation dialog, confirmed a fixture-only save and observed success feedback and the updated title.
- Edited and discarded a draft at phone width.
- Keyboard-adjusted the Material slider from 1 to 1.05 and observed its accessible value update.
- Expanded the Material inspector and switched to Interactions; ordered inventory and ground slots were visible.
- Navigated from overview to Cache Studio and inspected its populated fixture table and paginator.
- Entered a synthetic email and tabbed into the password control to inspect login labels and focus. No sign-in or CAPTCHA completion was performed.
- Browser console error log was empty for the editor interaction checks.

## Findings and comparison history

No actionable P0/P1/P2 visual findings remain within the revised Material-first scope. No visual fix loop was required after the combined comparison. Earlier custom-control styling and the stylesheet budget issue were removed during implementation before this comparison. Production build now has no budget warning.

P3 follow-up: tighten the existing mobile toolbar's account wrapping if a denser toolbar is desired. It remains visible and operable. Exact mock spacing is intentionally secondary to shared Material defaults.

## Validation and limits

- Admin suite: 19 files, 243 tests passed.
- Focused Item Forge tests cover Material inputs, readonly derived fields, inspector expansion/collapse and slider updates, alongside existing full-definition save and navigation behavior.
- Focused measured coverage after final formatting: 89.28% lines, 88.15% statements, 74.28% branches, 89.28% functions. Remaining uncovered behavior is pre-existing fallback/guard logic; templates and styles have browser evidence rather than inferred coverage.
- Production admin build passes without warnings. Biome has no errors; existing non-null assertion warnings remain. Angular tests retain the pre-existing polyfills compilation warning.
- Fallow audit passes with zero introduced findings and four inherited findings.
- Live authentication, live cache writes and actual 3D model content were not verified. The isolated preview uses fixture responses, and no production cache was changed.

final result: passed


Requirement coverage tests: `edits through Material inputs and keeps derived fields readonly`, `reveals the existing model preview through the Material inspector`, and `zooms the actual item preview and opens the full raw definition from quick actions`. The final focused coverage run passed all 15 tests. The line-count change from the earlier 92.3% result reflects source formatting; statement and branch coverage are unchanged.


---

# Cache resource navigation follow-up, 2026-10-31

Added the approved Definitions, Models and Sprites hierarchy using the shared Material tab navbar and existing routes. Definition-type tabs remain inside Definitions. The removed Cache Studio heading/subtitle remains absent. Current route ownership supplies selection; canceled guarded transitions retain the current tab.

Inspected actual browser rendering at 1651 x 953 CSS px and 390 x 844 CSS px. Desktop evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/cache-resource-tabs-desktop.png`. Phone evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/cache-resource-tabs-phone.png`. Both are density 1. Verified existing Models and Sprites destinations, return to Definitions, nested definition-type navigation and no horizontal page overflow at phone width. Browser console error log was empty. This checks the approved navigation addition, not a claim of pixel identity to the original mock.

The routing tests verify filtered/nested active sections and rejection by the existing dirty-draft guard. Admin suite passes 248 tests, production build passes, lint has no errors, strict OpenSpec passes and Fallow has zero introduced findings. The preview uses isolated fixtures; no live cache or authentication changes were made.

final result: passed


---

# Final navigation correction and legacy sprite retirement, 2026-10-31

The user's annotated screenshot explicitly removes the upper Definitions/Models/Sprites row and adds Models to the existing type row. This supersedes earlier two-level navigation QA. The cache shell now owns one outlined, icon-bearing Material type row; the definition browser no longer renders a duplicate. Models uses the same outlined search card, Material input/button and outlined result card as the other tools.

Verified one `nav[mat-tab-nav-bar]` in the browser, Models selected in the same row, and navigation to the current sprite browser and Sprite Studio. The legacy sprite manager component, template, stylesheet, test suite, route and orphaned metadata/search APIs are deleted. No compatibility page or route was added.

Desktop evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a19264-ebde-7101-9f0c-c53b7278cfed/single-cache-tabs-models.png`, a content-only crop of the browser showing the single row and matching model search structure. Phone evidence: `single-cache-tabs-phone.png` in the same directory, at 390 x 844 CSS px, density 1. Document width was 390px. The long row uses Material's native pagination controls.

234 tests pass, including `loads and reloads a model through the shared Material form`, `disables invalid model lookups without opening a preview`, `does not expose the retired sprite manager route`, `protects a staged PNG in the current Sprite Studio` and the shared navbar's guarded-transition cases. Changed-page coverage is 100% lines, 93.58% statements, 89.83% branches and 93.87% functions. The build and lint error gate pass; Fallow reports zero introduced findings. Existing test polyfills warning remains.

No actionable visual issue was found within the requested structure. The local preview is fixture-backed; live authentication, actual model rendering and cache persistence were not verified. No live data was modified.

final result: passed


---

# Model workspace reference implementation, 2026-11-01

The existing Angular Models page now uses the supplied editor composition: model header, inspection tabs, interactive geometry viewport, texture strip and properties/skin-group sidebar. This is an integration into the existing admin product, with its shared Material theme and cache-type navigation retained. It is not a claim of pixel identity to the standalone mock.

## Source and capture normalization

- Source visual truth: `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-e35b1d89-b75e-4378-b3fa-79c2744930a7.png`.
- Implementation URL: `http://localhost:4301/cache/models`.
- Evidence directory: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a192b4-4705-74c1-abce-e6998cda7f10/`.
- Final implementation screenshot: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a192b4-4705-74c1-abce-e6998cda7f10/model-workspace-desktop.png`.
- Full-view comparison: `model-workspace-comparison.png` in the evidence directory. Source and implementation were placed side by side in one image and opened together for review.
- Readable properties comparison: `model-properties-comparison.png` in the evidence directory. Source crop is x=1230, y=155, width=428, height=765; implementation crop is x=1246, y=261, width=380, height=680. Neither crop is scaled; different dimensions reflect the existing product layout and real data.
- Source and implementation are both 1672 x 941 pixels, desktop CSS viewport 1672 x 941, devicePixelRatio=1. No density resampling or browser chrome is included.
- State: model 666 loaded, Model tab selected, perspective camera, grid on, wireframe off, scale 128/128/128, rotation and offset zero, preview notes collapsed. Texture IDs and group counts differ because the implementation uses actual cache data.
- Phone evidence: `model-workspace-phone.png` and `model-workspace-phone-properties.png`, 390 x 844 CSS/pixel viewport. The settled document width is 390px; content width is 375px. Navigation collapses and properties stack below the viewport. An intermediate drawer animation was excluded from QA.

## Comparison history and findings

1. Initial comparison identified P2 excess vertical space and oversized property typography. Evidence is preserved in `model-workspace-before-density.png`, `model-workspace-before-density-comparison.png` and `model-properties-before-density-comparison.png`. The properties and texture strip were pushed below the desktop viewport. The lookup was moved into the header, toolbar/stats were overlaid on the canvas, the viewport height was adjusted to 430px, and the properties column widened to 380px. Material public overrides now set 12px property text, compact fields, subtle outline colors and denser checkboxes. Final full-view and focused comparisons were opened again after these fixes.
2. A P2 interaction defect was reproduced by entering scale 0 and clicking Reset preview: geometry stayed valid but invalid input text remained. The properties form now resets its Angular form state and the canonical transform values together. The browser confirms value 128 and aria-invalid=false after reset. Regression: `clears invalid transform text when resetting an unchanged preview`.
3. A P1 failure-recovery defect was found during cumulative flow review: a failed model request removed the projected lookup along with the model header. The lookup now remains available while loading and after failures. Browser evidence `model-workspace-lookup-error.png` shows an unavailable model with the lookup still visible; entering 666 and submitting with Enter recovers to the loaded workspace. Regression: `keeps lookup available after a failed request so another model can be loaded`.
4. The combined template initially exceeded the Fallow complexity gate. Focused workspace, property, texture and animation presentation components now compose the existing request owner. No request, bitmap or playback store was duplicated. Fallow now passes with zero introduced findings, without suppressions or configuration changes.

No actionable P0/P1/P2 issue remains within the recorded integration scope. The following differences are deliberate product/data constraints:

- The existing sidebar, toolbar, branded background and cache-type navbar remain. They account for the additional vertical offset versus the source mock.
- Original cache model 666 has 62 vertices, 54 triangles, one referenced texture (2), and no skin groups. The mock's four textures, 248 vertices, 426 triangles and invented bones are not substituted for actual data.
- The original cache mesh and texture were exported read-only through the existing .NET cache implementation to the evidence directory. No generated wall image or substitute texture was used. Existing brand assets and Material Symbols were reused.
- Save and Duplicate are disabled and explain the read-only model API. Name/description are inspection fields. Unsupported model mutation, texture upload, bone editing, alternate cameras and lighting gizmos were not fabricated.
- Recolor and retexture tabs explain definition ownership and offer the existing item-definition destination. Preview transforms are reversible local inspection values, not cache writes.

## Required fidelity surfaces

- Fonts and typography: existing Semplicita Pro and Material Symbols are retained. The title uses the shared headline scale with weight 600. Property values are 12px, verified from browser computed style. Description wrapping and small-label hierarchy were reviewed in the focused comparison.
- Layout and rhythm: viewport/sidebar/texture composition follows the reference. Panels use 8px corners and shared outline tokens; field corners use the public Material override. Wide content scrolls internally. Phone controls remain reachable without horizontal page overflow.
- Colors and tokens: shared dark admin surfaces, muted text, amber active states and focus outlines are reused. The inherited background and Material surface palette intentionally remain rather than adding a second theme.
- Image quality: a real WebGL canvas renders the decoded cache geometry and its original texture. The thumbnail uses the actual texture endpoint response. Differences in mesh orientation, counts and texture sharpness reflect real cache data and the existing inspection renderer. Client lighting/transparency limitations remain visible in Preview notes.
- Copy and content: headings and tabs follow the source. Unsupported operations and unavailable data use explicit explanatory copy. Skin groups are labeled as groups, not falsely represented as bones.

## Interaction and runtime verification

Verified in the in-app browser: load 666, keyboard form submission, failed lookup and recovery to another ID, orbit, grid toggle, wireframe toggle, focus/reset, valid scale/rotation updates, invalid-input reset, texture selection/material mode, metadata, recolor/retexture explanation tabs, empty animation state, and phone property editing/reset. `model-workspace-wireframe.png` records the real wireframe state. Exactly one canvas is present. Browser console error log was empty after the checks.

The browser uses an isolated local fixture server with exported real model/texture responses and a local fixture administrator session. Live authentication, live cache service connectivity and cache persistence were not verified. Application authorization and backend code were not changed.

## Automated validation

- `pnpm run admin:test:coverage`: 20 files, 241 tests passed. Overall collected coverage: 85.97% lines, 75.12% statements, 79.57% branches, 85.75% functions.
- `pnpm exec vitest run shared/model-viewer/model-scene.spec.ts`: 16 tests passed. A separate measured renderer coverage run reports 97.9% lines, 94.57% statements, 77.94% branches and 95.23% functions. GPU operations are mocked in that suite; browser evidence verifies actual WebGL rendering.
- Changed TypeScript coverage: preview owner 97.36% lines / 79.05% branches; workspace 100% / 73.91%; properties 100% / 92.85%; animation controls 100% / 100%; Models page 100% / 95%; texture panel 53.33% / 50%. The texture panel's inline template/style ranges and some material-label alternatives remain uncovered by automation; Opaque selection is verified in the browser and Cutout by a regression test. External template coverage mapping is incomplete, so no blanket UI-coverage claim is made.
- Production `pnpm run admin:build` passes without compiler/style-budget warnings.
- `pnpm run lint:biome` passes its error gate; 15 inherited warnings and two inherited informational diagnostics remain. No new warning is introduced.
- Fallow audit passes with zero introduced dead-code, complexity, duplication or styling findings. Two test-boundary input findings and the existing renderer complexity finding remain inherited.
- Strict `openspec validate model-workspace-reference --strict` and `git diff --check` pass.
- The existing Angular test polyfills compilation warning remains. One intermediate test used a harness action that waited on its own pending HTTP request; that test was corrected to flush the request before waiting for stability. An interrupted coverage process briefly held the report directory; the final sequential run completed cleanly.

| Requirement | Evidence |
| --- | --- |
| Validated lookup and reload | `loads and reloads a model through the shared Material form`; `disables invalid model lookups without opening a preview` |
| Recover after failed lookup | `keeps lookup available after a failed request so another model can be loaded` |
| Actual properties, groups and unavailable writes | `shows actual properties and skin groups and explains unavailable writes` |
| Local transform/reset without writes | `transforms and resets rendered geometry without changing the source or issuing writes`; `clears invalid transform text when resetting an unchanged preview` |
| Tabs and display settings preserve the viewer | `switches inspection tabs without replacing the viewer and forwards display toggles` |
| Model replacement and retry | `resets inspection state on replacement and presents retryable model failures` |
| Actual texture metadata | `inspects a referenced texture using its actual material mode` |
| Renderer resource ownership | `toggles the grid and wireframe across mesh replacement and disposes helpers` |

## Follow-up polish

P3 only: a more compact thumbnail caption treatment could reduce the final strip's vertical height. Exact shader lighting, mock-only tools and backend editing are outside this change, not unfinished implementation tasks.

## Implementation checklist

- [x] Integrate the reference composition into the existing Models page.
- [x] Retain one request/playback/bitmap owner and shared renderer defaults.
- [x] Implement usable inspection, transform, validation and recovery controls.
- [x] Fix the recorded P1/P2 findings and compare the revised captures.
- [x] Verify desktop/phone rendering, automated tests, build, lint and Fallow.
- [x] Keep the local preview open; do not publish or modify cache data.

final result: passed


---

# Model properties field removal, 2026-11-01

Applied the annotated correction from `C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-bf8a06a0-2a08-4903-99a4-8d4fabb8e4ab.png`: removed Model ID, Name and Description from the properties sidebar and deleted their unused layout rule. The panel now starts with Model scale. Model identity remains in the existing page header and structure. No application logic changed.

Verified the rendered panel in the in-app browser using exported model 666. Evidence: `C:/Users/fmavd/.codex/visualizations/2026/10/31/01a192b4-4705-74c1-abce-e6998cda7f10/model-properties-simplified.png`. The three fields are absent; scale, rotation, offset, reset, options and structure remain visible. Production admin build passes.

final result: passed
