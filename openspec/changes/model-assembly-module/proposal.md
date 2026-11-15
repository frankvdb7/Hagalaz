# Proposal

## Why

Model assembly and item icon composition are client rules that currently live inside HTTP endpoint classes in `Hagalaz.Services.Cache`, and they reach into each other across those classes:

- **Assembly lives in an endpoint.** `ModelEndpoints.BuildModel` (merging parts, scale and offsets, recolour and retexture, resolving billboards, effectors and particles) returns the HTTP DTO `GetModelDto`. It takes 21 parameters and has four callers: three endpoints and `ModelImages`.
- **Icons reach into endpoints.** `ItemIcons` calls the `internal static` members `ModelEndpoints.InventoryAppearance` and `ModelEndpoints.BuildItemModel`. `ModelImages` uses `ItemIcons.TextureSource` and `ItemIcons.EncodePng`.
- **Object placement lives in an endpoint.** `GetObjectRenderAppearance` holds the client's placement rules (transform variant at zero varp/varbit state, the shape-to-model-shape mapping, orientations, mirroring, floor shadow strength). `ShadowStrength` reads the DTO's positions. The batch endpoint gets each row's status by inspecting `IResult` objects.
- **Cache key inputs drift.** The ETag validator's `SourceIndexes = [19,16,7,8,13,9,26]` is kept by hand in `ItemIcons`, away from the reads it describes.
- **Duplicated reads.** The material table (index 26) is read and decoded in `TextureSource` and in three `TextureEndpoints` handlers. Index numbers (7, 27, 29, 26, 9, 13, 8, 2) are literals spread through endpoint code.
- **Hard to test.** All about 50 tests for these rules go through the HTTP test host. None can check a placement or icon layering rule directly.

**Goal:** two modules in the Cache library own these client rules: **model assembly** (`ModelAssembler`) and **item icon composition** (`ItemIconComposer`). Endpoints keep only input validation, DTO mapping, fixed disclaimer text, PNG encoding and HTTP caching. Every HTTP response stays byte-identical.

## What Changes

- **Add `ModelAssembler`** (`Hagalaz.Cache/Logic/Rendering/`), a sealed class with no interface, registered in `AddGameCache`. It produces an `AssembledModel` from six entry points: `Model(modelId)`, `ItemAppearances(item)`, `Item(itemId, appearanceKey)`, `Npc(npcId)`, `ObjectShape(objectId, shape?)` and `ObjectPlacement(objectId, shape, rotation)`. Placement results also carry size, decor displacement, contouring, shadow strength and clipping fields.
- **Add `ItemIconComposer`** (same folder, same registration): `Render(itemId, IconOptions) → Result<int[]>` ARGB pixels, plus a static `SourceIndexes` declared next to the named index constants the icon reads.
- **Move `TextureSource`** to `Hagalaz.Cache/Logic/Rendering` as a public class that lives for one render and has an injected `ISpriteTypeCodec` for the font. `TextureEndpoints` reads the material table and looks up materials through it.
- **Results:** expected outcomes are typed FluentResults errors: absent subject (no shapes, no models, no available appearance), unknown appearance key, unsupported appearance (inverted object). A hidden transform variant is a successful placement with no parts. Endpoints map the errors to today's 404, 400 and 422 responses with today's titles and details. Cache read faults keep propagating to `CacheExceptionHandler`.
- **Warnings:** the modules return the warnings they discover in the data (texel fallback, unavailable billboards or effectors, unsupported palette or colour overrides, contouring mode, render-animation decode failure, substituted or hidden transform variant). The fixed disclaimer text stays in the endpoints, which build today's warning arrays in today's order.
- **Endpoints and glue:**
  - `ModelEndpoints`, `ItemDefinitionEndpoints` (image) and `TextureEndpoints` call the modules;
  - `ModelImages` stays in the service as thin glue (assembler, then `TextureSource`, then `ItemIconRenderer`, then PNG encoding);
  - the batch placement endpoint maps typed errors per row;
  - `ItemIcons` is reduced to the ETag validator (using `ItemIconComposer.SourceIndexes`, keeping the per-cache random generation and `InvalidateValidators`) and PNG encoding.
- **Constants:** index and archive numbers become named constants inside the modules. A later change can replace them with definition archives.

**Reused, not added:** `ModelGeometryBuilder`, `ItemIconRenderer`, `ClientFont`, `TextureImageConverter`, the existing codecs and `ITypeProvider<T>`s, FluentResults (already in `Hagalaz.Cache`), `CacheExceptionHandler`, `RenderedImageResponse`. No new cache, worker, lock or dependency. ImageSharp stays in the service.

### Non-goals

- Any change to HTTP routes, DTOs, JSON, PNG bytes, ETags or status codes.
- Definition archives, or moving index numbers out of the modules (architecture candidates 1 and 2).
- Animation frame and sequence endpoints, widget programs, and the texture image, HDR and material response bodies beyond the shared material lookup.
- A process-wide texture cache, or caching assembled models.
- Removing the existing HTTP tests.
- Fixing assembly inaccuracies that are already documented in warnings (variant transforms, palette overrides, inverted objects).

### Acceptance criteria

1. `ModelEndpoints` has no `BuildModel`, `AssembleModel`, `PrepareObjectPart`, `ReplaceValues`, `ShadowStrength`, `ReadBillboards`, `ReadEffectors`, `ReadParticles` or `ReadMember`. `ItemIcons` has no `Render` or `TextureSource`. No service class calls `internal static` members of an endpoint class.
2. No literal cache index or archive number for models, attachments, materials, textures, fonts or sprites appears in `Hagalaz.Services.Cache` model, item image or texture code. Each one appears once, as a named constant in the module that reads it.
3. The ETag validator for item, model, widget-model and object images takes its index list from `ItemIconComposer.SourceIndexes`.
4. Every existing test in `Hagalaz.Services.Cache.Tests` and `Hagalaz.Cache.Tests` passes unchanged.
5. A temporary real-cache comparison shows the old and new code produce identical JSON, PNG bytes, ETags and status codes for a sample of model, item (each appearance), NPC, object shape and object placement (every shape and rotation) requests, and for item icons with each option.
6. New `Hagalaz.Cache.Tests` cases cover through the modules' interfaces: placement shape mapping, orientations and mirroring, transform variant choice, shadow strength, item recolour and retexture, icon layering for noted, lent and bought items, stack variants, the quantity label, discovered warnings and each typed error.
7. `dotnet build Hagalaz.sln` adds no warnings.

### Stop conditions

- **Object image offsets.** If a PNG byte test shows the object image differs when assembled with the definition's offsets, keep a documented assembly option for image framing instead of unifying them. The same rule applies to the model image's 4× unit scaling against the `GET /models/{id}` form.
- **Old and new code disagree.** If the real-cache comparison finds any difference, stop and report it before deleting the old code.
- **Scope grows.** If moving a flow would need a change to a DTO, a codec or a type provider, stop and propose a follow-up change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure refactor with no spec-level behaviour change (`skip_specs: true`).

## Impact

- **`Hagalaz.Cache`:**
  - new `Logic/Rendering/ModelAssembler.cs` (with `AssembledModel`, the placement result and the errors), `Logic/Rendering/ItemIconComposer.cs` and `Logic/Rendering/TextureSource.cs`;
  - `AddGameCache` registers `ModelAssembler` and `ItemIconComposer` (`ISpriteTypeCodec` is already registered).
- **`Hagalaz.Services.Cache`:**
  - `ModelEndpoints`, `ModelImages`, `ItemIcons`, `ItemDefinitionEndpoints`, `TextureEndpoints`, `EndpointErrors`;
  - `ModelCodecs` loses the members only assembly uses, if no endpoint needs them.
- **Tests:** new `ModelAssemblerTests` and `ItemIconComposerTests` in `Hagalaz.Cache.Tests`. Existing HTTP tests are unchanged.
- **Docs:** CONTEXT.md already has *Model assembly*, *Object placement* and *Item icon*.
- **HTTP:** unchanged.
