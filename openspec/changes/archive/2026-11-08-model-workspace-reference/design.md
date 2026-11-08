# Design

## Context and boundary

The raw model API only maps GET operations. CacheModelPreviewComponent already owns model, texture, bitmap lifecycle and animation requests. ModelsManagerPageComponent owns validated model selection. ModelScene owns camera/GPU resources. Keep those owners.

## Decisions

- Add an opt-in ModelWorkspaceComponent presentation to CacheModelPreviewComponent, retaining compact rendering for other consumers. Properties, textures and animation controls have focused presentation components. The preview remains the sole request/bitmap/playback owner; the workspace owns local inspection transforms and selection. Do not add another data store or loader alongside the existing resources.
- Use actual model geometry and referenced texture images instead of generating replacement imagery. Reuse installed Material Symbols and Semplicita Pro, which the admin design requires.
- Keep local inspection tab, transform and selection state in the component. Reset state when model identity changes. Transform the posed preview geometry only; never write to the loaded DTO or a backend.
- Add optional grid/wireframe display inputs to the shared renderer, defaulting off. ModelScene owns helper construction/disposal and reapplies settings when replacing a mesh.
- Render source metadata read-only. Skin IDs are groups, not a skeletal hierarchy. Recolor/retexture tabs explain their definition-level ownership. Save/duplicate remain disabled because no write endpoint exists.
- Retain the shared admin shell and cache-type navigation. Responsive layout stacks properties below the viewport; wide metadata remains internally scrollable.

## Alternatives

A standalone prototype would duplicate the application and was rejected by the user. A second model fetch/store would duplicate ownership. Backend editing would substantially expand scope beyond image-to-code. Hardcoded reference model/texture assets would conceal actual cache content.

## Invariants and failures

Lookup never accepts negative/nonintegral IDs. Requests and retries remain owned by the existing resources. Missing images and model/animation errors remain visible and retryable. Changing tabs does not destroy playback or GPU resources. Invalid transform input cannot produce nonfinite geometry. Destroying the renderer releases helpers as well as existing resources.

## Verification

Focused component and renderer tests, full admin tests/coverage, build, Biome, Fallow, strict change validation, and desktop/phone browser evidence. Fixture-backed checks must be distinguished from live cache verification.
