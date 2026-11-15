# Design

## Context

See proposal.md, "Why". As things stand:

- **Assembly.** `ModelEndpoints.BuildModel` builds a `GetModelDto` from model IDs and many definition values. `GET /models/{id}` does not use it. That handler maps `ModelGeometryBuilder` output directly, with unscaled positions and `CoordinateScale: 0.25` for models older than version 13. `BuildModel` instead multiplies old-version positions by 4.
- **Images.** `ModelImages` assembles through `BuildModel`. For a model it uses `[id]` and scale 128. For an object it uses the definition's scale, but offsets 0,0,0 and no animations. It then rasterises through `ItemIconRenderer`.
- **Icons.** `ItemIcons.Render` recursively composes an item, its lent or bought original and its note content (depth ≤ 1), then the quantity label. It uses one `TextureSource` per request, which remembers the material table, textures and the `p11_full` font.

## Goals / Non-Goals

**Goals:**
- One owner for each client rule: `ModelAssembler` for model assembly and object placement, `ItemIconComposer` for item icons.
- Both can be tested over `MemoryFileStore` without an HTTP host.

**Non-Goals:**
- Changing what any rule computes (see proposal.md, "Non-goals").

## Decisions

### Two modules, both concrete, in `Hagalaz.Cache/Logic/Rendering`

`ModelAssembler(ICacheAPI, ITypeProvider<IItemType>, ITypeProvider<INpcType>, ITypeProvider<IObjectType>, codecs…)` and `ItemIconComposer(ICacheAPI, ModelAssembler, ITypeProvider<IItemType>, ITypeCodec<IMaterialTableType>, ISpriteTypeCodec)` are sealed classes registered transient in `AddGameCache`, like `TypeMutator<T>`.

- *Alternative: interfaces.* Each would have one adapter, so the seam would be hypothetical. Tests use the real modules over `MemoryFileStore`.
- *Alternative: one module.* Icon composition is a separate client rule that sits on top of assembly. Merging them would widen the assembly interface with pixel options.
- *Alternative: keep them in the service.* The cache reads and the rendering primitives are already in `Hagalaz.Cache`, and `Hagalaz.Cache.Tests` already tests rendering (`ItemIconRendererTests`, `ModelParticleTests`).

### `AssembledModel` replaces `GetModelDto` as the assembly output

`Hagalaz.Cache` cannot reference service DTOs. `AssembledModel` holds exactly the fields `BuildModel` fills today: geometry arrays, skins, texture data, priorities, shading, ambient, contrast, scale, first-texel faces, animation IDs, the object animation pose, resolved particles, billboards and effectors, and discovered warnings.

Resolved attachments (`ModelParticle`, `ModelBillboard`, `ModelEffector`) carry the decoded Types (`IParticleEmitterType`, `IBillboardType`, `IParticleEffectorType`) plus the warning found while reading them. Endpoints map those Types to the DTOs. *(Changed during implementation: mirroring every particle and effector DTO field in `Rendering` would have duplicated the HTTP contract. The only values the module derives are the warnings, such as an unsupported effector scope.)*

Endpoints map `AssembledModel` to `GetModelDto` field for field. The first warning is the endpoint's fixed summary followed by the model's `Notes`, then come the discovered `Warnings`, which is today's order and wording. *(Added during implementation: the item palette-override note and the NPC render-animation failure have always been appended to the summary sentence, not given entries of their own, so `AssembledModel.Notes` keeps them separate from `Warnings`.)*

### Entry points

| Entry point | Replaces | Result |
| --- | --- | --- |
| `Model(modelId)` | `ReadModel` | `AssembledModel` (stored units, coordinate scale) |
| `ModelInClientUnits(modelId)` | the model and widget images' `BuildModel([id])` | `AssembledModel` |
| `ItemAppearances(IItemType)` | `ItemAppearances` | appearance list (key, label, model IDs, offsets, availability) |
| `Item(itemId \| IItemType, appearanceKey)` | `GetItemModel`, `BuildItemModel` | `Result<ItemModel>` (appearance + model) |
| `Npc(npcId)` | `GetNpcModel` | `Result<AssembledModel>` |
| `SelectObjectShape(objectId, shape?)` | the checks in `GetObjectModel` and `GetObjectImage` | `Result<ObjectShapeSelection>` |
| `ObjectShape(selection)` / `ObjectShapeInImageFrame(selection)` | `GetObjectModel` / `RenderObjectPng` | `AssembledModel` |
| `ObjectPlacement(objectId, shape, rotation)` | `GetObjectRenderAppearance`, `PrepareObjectPart`, `ShadowStrength` | `Result<ObjectPlacement>` (base fields, parts, warnings) |

Private steps (merging parts, recolouring, resolving attachments, reading archive members by member ID) stay inside the class. This avoids exposing a 21-parameter `Parts` method again.

**Changed during implementation: selecting an object shape is separate from assembling it.** The object image endpoint answers 404, 422 or 304 before rendering, so the cheap selection (definition, shape and model lookup, inverted check) has to run before the validator, and assembly runs only inside the render callback. A single `ObjectShape(objectId, shape?)` would have assembled on every revalidation.

**Image framing kept explicit (stop condition applied).** The widget camera uses absolute zoom, so widget images need client units (old models ×4), which differ from the `GET /models/{id}` form. Model images and object images therefore keep today's inputs through `ModelInClientUnits` and `ObjectShapeInImageFrame`, with offsets 0 and no animations. The PNG byte experiment for unifying the fitted model and object images was not run. The real-cache comparison (task 4.4) shows the explicit forms are byte-identical to the old code.

### Outcomes: typed errors for expected cases, exceptions for faults

Errors are `ModelSubjectAbsentError` (no shapes, no models for the shape, no available appearance), `UnknownAppearanceError` and `UnsupportedAppearanceError` (inverted object, with today's detail text). A hidden transform variant succeeds with no parts and the "Hidden for …" warning.

`EndpointErrors.ModelAssemblyFailure` maps these to 404, 400 and 422 with today's bodies. `IOException`, `FileNotFoundException` and decode failures keep propagating to `CacheExceptionHandler`. The batch endpoint keeps calling `EndpointErrors.MapException` for those. The NPC render-animation decode failure stays a discovered warning, as today.

- *Alternative: exceptions or nulls throughout.* That hides why a subject is unavailable and differs from `TypeMutator` and `RegionMapArchive`.

### Warnings split by origin

The modules report what they find in the data. Endpoints own the fixed preview disclaimers ("Client model lighting uses a fixed inspection light…"), because those describe the preview feature, not the cache.

### Index constants and `SourceIndexes`

Each module declares named constants for the indexes and archives it reads: models 7, the render animation archive 2/32, particle emitters and effectors 27/0 and 27/1, billboards 29/0, the material table 26/0/0, textures 9, font metrics 13 and sprites 8.

`ItemIconComposer.SourceIndexes` is built from those constants plus the item (19) and object (16) definition indexes, giving today's list `[19,16,7,8,13,9,26]` in today's order, because the ETag hash depends on the order. The service validator keeps `RendererVersion`, the per-cache random generation and `InvalidateValidators`.

- *Alternative: record the reads at runtime.* The validator has to compute the key before deciding whether to render.

### `TextureSource` lives for one render and moves to `Rendering`

It is public, built from `ICacheAPI`, the material codec and `ISpriteTypeCodec`, and is created once per icon or image render, exactly as today. It exposes `Material(id)` (which reads the table once) for `TextureEndpoints`, `For(faceTextureIds)` and `Font()`.

- *Alternative: a shared process-wide cache.* That would be a second cache that writes have to invalidate, with no measured need.

### State and ownership

Neither module holds state between calls. Cache writes, locks and invalidation stay owned by `CacheApi`. ETag generation stays owned by the service. There is no retry or reconciliation owner because nothing here retries.

## Risks / Trade-offs

- [Byte drift while moving the arithmetic, such as integer scaling order or warning order] → Move the code verbatim first and keep the HTTP tests unchanged. A temporary real-cache comparison (task 4.1) covers every shape and rotation and a sample of models, items, NPCs and icons before the old code is deleted.
- [The ETag key changes if `SourceIndexes` order or contents change] → A test pins the list to `[19,16,7,8,13,9,26]`.
- [`AssembledModel` duplicates the `GetModelDto` shape] → This is accepted: one is the domain result and the other the HTTP contract. The mapping is a single function.
- [`Item`/`Npc`/`Object` providers return cast concrete types (`(ItemType)`, `(NpcType)`, `(ObjectType)`) for fields not on the interfaces] → Keep the casts inside the module. Widening the abstractions is out of scope.

## Migration Plan

This is an in-process refactor with no data or contract migration. Rollback means reverting the commit.
