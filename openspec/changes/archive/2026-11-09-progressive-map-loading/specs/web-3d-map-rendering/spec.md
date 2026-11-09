# Spec delta

## ADDED Requirements

### Requirement: Map scenes become usable before every asset finishes loading

The Maps detail MUST display available terrain and incrementally add visible landscape and textures while other work remains pending. A slow or failed region or asset MUST NOT prevent other available scene data from appearing. Loading progress MUST distinguish pending work, completion and failures.

#### Scenario: A placement definition has only state-driven model variants
- **GIVEN** object 12163 shape 10 has no base model and its zero-valued varbit selects object 12144
- **WHEN** a single or batch render appearance is requested
- **THEN** the preview uses the selected definition's geometry and render metadata while retaining source ID 12163 and placement shape/rotation
- **AND** a warning identifies the chosen variant and assumed static state
- **AND** a selected hidden (-1) variant returns empty geometry successfully rather than choosing another variant or reporting a missing appearance

#### Scenario: One selected region is slower than another
- **GIVEN** multiple selected regions are loading
- **WHEN** one render response arrives while another remains pending
- **THEN** the available region appears without waiting for the pending region
- **AND** its floor textures can load without waiting for landscape appearances
- **AND** later region arrival preserves the camera and updates shared terrain boundaries

#### Scenario: Landscape is still arriving
- **GIVEN** terrain is available and several landscape appearance batches are pending
- **WHEN** a batch finishes
- **THEN** its available landscape becomes visible before unrelated batches finish
- **AND** incomplete textures retain meaningful decoded colours
- **AND** progress remains readable outside the viewport
- **AND** scene and appearance warnings remain in the render responses and map store without a workspace warnings panel

#### Scenario: A visible texture loads
- **WHEN** the map loader requests a visible texture image
- **THEN** the image response includes the complete existing material settings
- **AND** the map uses one HTTP request for that texture without a separate material request
- **AND** transparency, tint, brightness, effects, scrolling and repetition remain unchanged

#### Scenario: A previously downloaded texture is requested again
- **GIVEN** the browser has a cached texture image and its validator
- **WHEN** the source image and material settings remain unchanged
- **THEN** a conditional request returns 304 before PNG conversion, allowing reuse of the cached image
- **AND** cached images are private and require revalidation with existing authorization
- **AND** changed source bytes or material settings return an updated image, metadata and validator

#### Scenario: A texture is requested without a browser validator
- **WHEN** an authorized client repeats an unconditional texture GET within the output cache lifetime
- **THEN** the service may reuse the successful response from its bounded ASP.NET Core output cache
- **AND** conditional requests continue to validate current image and material data

### Requirement: Map asset loading follows current visibility

The Maps detail MUST initially request appearances and textures needed by visible terrain planes and landscape. Showing another plane or landscape MUST load its missing assets without refetching unchanged region data. Already-loaded assets MUST be reused during repeated visibility changes within the same selection.

#### Scenario: Only the ground plane is initially visible
- **WHEN** a map opens with plane zero visible
- **THEN** hidden-plane-only appearance keys and textures are not requested eagerly
- **AND** hiding landscape avoids requesting its still-unstarted exclusive assets

#### Scenario: Another plane is shown during loading
- **GIVEN** a selection already has decoded region data and some completed assets
- **WHEN** the administrator shows another plane
- **THEN** missing assets for that plane join the bounded loading work
- **AND** completed region and appearance reads are not duplicated
- **AND** switching back to an already-loaded plane reuses its assets

### Requirement: Batch appearance reads preserve independent outcomes

The cache read API MUST accept bounded object appearance batches with cache administrator authorization. Each valid requested key MUST retain its own success or failure outcome, and successful geometry MUST agree with the existing single-appearance contract. Invalid input MUST be rejected before asset decoding. Cancellation MUST stop remaining work.

#### Scenario: A batch contains a missing or unsupported appearance
- **GIVEN** a valid batch includes supported and unsupported appearance keys
- **WHEN** the batch is read
- **THEN** supported appearances are returned with their source keys
- **AND** missing or unsupported entries have explicit per-key failures
- **AND** one failed appearance does not discard the other results

#### Scenario: Batch bounds or placement values are invalid
- **WHEN** a batch is empty, exceeds its documented limit or contains invalid IDs, shapes or rotations
- **THEN** the API returns a validation error before model decoding
- **AND** unauthenticated or unauthorized requests retain existing cache access restrictions

### Requirement: Progressive updates reuse resources and retain selection ownership

Progressive map updates MUST use bounded concurrency and coalesced scene publications. Unchanged prepared assets and GPU textures MUST be reused. The active selection MUST remain the single owner of pending requests, decoded bitmaps, retry and cleanup; obsolete results MUST NOT reappear.

#### Scenario: A rendered object contains genuinely nonfinite texture projections
- **GIVEN** object 45291 shape 10/model 48743 contains 47 faces whose client projection coordinates are nonfinite
- **WHEN** a rendering endpoint prepares its appearance
- **THEN** the object remains available with explicit face fallback metadata and a warning
- **AND** only those faces sample the original texture's first texel independently of animated UV transforms, retaining material alpha
- **AND** valid UVs, texture IDs, geometry and source-face references remain unchanged
- **AND** strict decoding without the rendering opt-in continues to reject invalid projections

#### Scenario: Camera moves through a populated map
- **GIVEN** opaque and cutout faces share materials throughout the scene
- **WHEN** geometry is prepared for rendering
- **THEN** opaque/cutout triangles are batched by material without changing source geometry, face winding or attributes
- **AND** camera movement only recomputes ordering for transparent faces
- **AND** transparent faces retain client priority, camera depth and source-order ties
- **AND** animation and particle references continue to address source topology

#### Scenario: A wall texture projection has an unused zero-scale axis
- **GIVEN** object 33858 shape 0 uses model 36668 with a box projection whose unused Y scale is zero
- **WHEN** its render appearance is decoded
- **THEN** box-side selection uses unscaled orientation and the valid projected texture coordinates remain finite
- **AND** ordinary projection coordinates remain unchanged
- **AND** projections requiring a zero-scale UV axis still fail explicitly

#### Scenario: A sloped wall uses a collapsed-X box projection
- **GIVEN** object 45092 shape 0 uses model 48778 with zero-X box mappings on sloped faces
- **WHEN** a single or batch appearance is requested in any of the four rotations
- **THEN** the client negative-X projection side is preserved and the response contains finite textured geometry
- **AND** the earlier object 33858 shape 0 remains supported

#### Scenario: A ground decoration uses a collapsed-Y box projection on sloped faces
- **GIVEN** object 45152 shape 22 uses model 48859 with a zero-Y box projection and nonzero oriented Y normal
- **WHEN** single or batch appearances are requested in any rotation
- **THEN** the valid X/Z projection is used even when Y is not the dominant normal component
- **AND** all texture coordinates remain finite without substituting placeholder geometry

#### Scenario: Additional textures and appearances arrive
- **GIVEN** some scene textures and prepared geometry are already available
- **WHEN** another batch is published
- **THEN** unchanged textures are not disposed and uploaded again
- **AND** terrain preparation is not repeated solely because object appearances changed
- **AND** camera framing is not reset for every asset batch

#### Scenario: A selection is replaced or the detail is left
- **WHEN** a new selection replaces pending work or navigation leaves the map detail
- **THEN** old requests and scheduled publications are cancelled
- **AND** late image completions are closed without becoming current assets
- **AND** owned CPU/GPU resources are released exactly once

#### Scenario: A partially loaded selection is retried
- **GIVEN** available data is visible and some work failed
- **WHEN** the administrator retries the current selection
- **THEN** retry uses the existing selection owner without duplicate scene placements
- **AND** progress and failures describe the active attempt

### Requirement: Terrain base colours use the client palette
The viewer SHALL convert floor RGB through the client integer HSL packing and software colour palette with deterministic gamma 0.7 before linearising it for rendering.

#### Scenario: Region 12850 dark earth and grass
- **WHEN** ordinary or blended terrain is prepared before or after its texture loads
- **THEN** its base tint uses the same client palette conversion, including underlay 164 earth and underlay 160 grass
- **AND** geometry, source-plane visibility, texture coordinates and blending weights remain unchanged

### Requirement: Terrain tint follows surrounding underlays
Underlay tints SHALL average client HSL components across the available same-plane neighbourhood before palette conversion. Blended terrain SHALL apply this spatial vertex tint once without creating a material per tile.

#### Scenario: Dark floor next to lighter earth
- **WHEN** surrounding underlays affect the client's averaged tint
- **THEN** ordinary and blended terrain use the averaged colour with unchanged texture weights and geometry

### Requirement: Ordinary texture arrival reuses prepared terrain
Arriving ordinary opaque floor textures SHALL reuse prepared terrain positions, colours, UVs and blending weights. Geometry SHALL retain declared texture IDs while the renderer uses its untextured fallback until images are available.

#### Scenario: Cached floor images finish decoding
- **WHEN** ordinary opaque floor texture assets arrive or are replaced
- **THEN** terrain preparation is reused without recalculating neighbourhood colours
- **AND** a transparency or special-effect mode change still invalidates affected terrain preparation

### Requirement: Texture arrival preserves active renderer resources
The renderer SHALL retain unchanged geometry and active material instances during progressive image arrival. Terrain texture availability SHALL be updated through uniforms without creating shader variants for each arrival order. Retired resources SHALL be disposed once, while live textures, material effects and rendering semantics are preserved.

#### Scenario: Images arrive while terrain is visible
- **WHEN** a previously missing terrain image arrives
- **THEN** existing terrain materials and unchanged geometry remain alive and their sampler uniforms update
- **AND** shader program identity is independent of which images have arrived

#### Scenario: A material is replaced or leaves the scene
- **WHEN** shader inputs or selected geometry require a different material
- **THEN** the required material is selected and retired resources and effect callbacks are released without disposing live resources

### Requirement: Image arrivals reuse unchanged landscape geometry
The map viewer SHALL reuse its combined terrain and landscape geometry for ordinary texture-only publications when decoded geometry colours are unchanged. It SHALL rebuild when regions, visible planes, appearances or relevant decoded colour settings change, retaining warnings and existing legacy colour fallback semantics.

#### Scenario: An ordinary image finishes loading
- **WHEN** an image arrives or is replaced with neutral or unchanged decoded colour settings
- **THEN** the combined geometry and its prepared model parts retain identity
- **AND** the renderer updates images without re-expanding every landscape placement

#### Scenario: A material changes model colour
- **WHEN** colour blending, brightness or effect-6 lighting changes
- **THEN** affected prepared colours and combined geometry are rebuilt correctly

### Requirement: Sorted transparent runs share contiguous draw ranges
The renderer SHALL write transparent faces into contiguous draw-index ranges in established client priority, depth and source-tie order, batching only adjacent equal-material runs. Source indices and vertex attributes SHALL remain unchanged, and camera or alpha updates SHALL retain the same visible ordering.

#### Scenario: Transparent faces share a material after sorting
- **WHEN** camera depth sorting makes same-material transparent faces consecutive
- **THEN** they share a contiguous draw group without changing the priority or depth order
- **AND** source topology remains available unchanged for animation and particles

### Requirement: Texture interest does not repeatedly scan model faces
The selection loader SHALL discover texture IDs once per immutable appearance and reuse its unique IDs while interest is recalculated. Repeated placements SHALL not repeat appearance texture discovery. Visibility, bounded requests, progress and selection resource ownership SHALL remain unchanged.

#### Scenario: Repeated objects and progressive texture arrivals
- **WHEN** multiple visible placements share an appearance and texture completions recalculate interest
- **THEN** their model face texture arrays are scanned once per selection while all required unique textures load once
- **AND** a new selection discovers its appearance textures independently

### Requirement: Landscape assembly preserves attributes with bounded temporary allocations
Landscape expansion SHALL reuse temporary attribute values and batch its zero terrain-weight padding while preserving the complete output geometry and immutable source data.

#### Scenario: Multiple landscape parts follow terrain
- **WHEN** landscape vertices are appended after terrain
- **THEN** every output vertex retains its position, colour, UV, opacity and first-texel marker
- **AND** terrain blend weights are preserved for terrain draw groups, and landscape vertices do not allocate terrain weights
