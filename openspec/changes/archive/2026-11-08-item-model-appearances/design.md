# Design

## Context and decisions
ItemTypeCodec already decodes worn/head references and wear offsets. ItemDefinition.method6292 in GameClient selects a primary model, includes tertiary only when secondary exists, normalizes old models by shifting two bits, merges parts, translates by decoded wear offsets and applies recolors/retextures. It does not apply inventory scale. ItemDefinition.method6295 head assembly similarly normalizes and merges two parts without worn translation. Inventory remains the default.

Use one metadata projection for selecting assembly parts and presenting choices. Keep five fixed keys: inventory, male-equipped, female-equipped, male-head, female-head. Metadata reports absent primary as unavailable even when a secondary is present. Add an optional appearance query to the existing model route and a sibling model-appearances route; both inherit existing authorization. Existing AssembleModel offsets are multiplied by four, whereas item codec wear offsets are already shifted by two; pass offsets divided by four at that boundary. Preserve existing geometry and replacement ownership.

The existing CacheModelPreview owns selected appearance and metadata requests. Item changes reset selection to inventory. Renderer and GPU ownership stay unchanged. Display all choices, disabling unavailable ones, with source IDs for the selected choice. Failed metadata has retry. Full character bodies and customization overrides remain outside this preview.

## Validation
Production-codec fixtures prove three-part selection, gender offsets, two-part heads, absent primary and ignored orphan tertiary, invalid/missing selections and inventory compatibility. Component HTTP tests prove selection/IDs and reset/retry. Both Angular builds and affected .NET tests run; browser checks are waived.

## Completed verification

The selector uses linkedSignal with item ID as its source, so navigation resets to inventory before model request selection. A component regression initially exposed an effect-based reset issuing the new item's request with the old appearance; synchronous source reset removes that obsolete request. No extra state owner or retry mechanism was added.

Validation: 64 cache-service tests, 233 admin tests and 30 shared viewer tests pass. API fixtures cover both genders, three-part worn/two-part head assembly, translations applied once to every part, absence of inventory scaling, ordered recolor/retexture behavior, unavailable primary references and orphan tertiary references. Existing inventory requests remain covered. Component tests cover model-ID display, unavailable choices, selection, navigation reset and metadata retry. The routing suite stubs the preview boundary while the preview suite owns its asset HTTP assertions. Both Angular production builds pass; the affected .NET projects build through tests and Aspire rebuild, and the running cache resource is healthy. Existing package vulnerability/pruning and test-polyfill warnings remain; no new compiler diagnostics were introduced.

Browser checks remain waived by the user. These are isolated item models, without player body assembly, body-part hiding, customization or player-animation association. New read-only routes inherit CacheAdminPolicy. No cache mutation, dependency changes, commits or pushes. Prior uncommitted work and the existing package-lock change were preserved.
