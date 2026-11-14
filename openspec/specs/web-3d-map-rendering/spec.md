# web-3d-map-rendering Specification

## Purpose
Specify how Hagalaz provides semantic terrain data for web-based 3D map rendering.

## Requirements

### Requirement: Rendering uses authoritative decoded terrain data

Hagalaz MUST decode RuneScape terrain rendering semantics on the server and MUST expose semantic terrain data to the web UI without requiring the browser to decode raw cache containers.

#### Scenario: A region is requested for 3D terrain rendering
- **GIVEN** a cache region is available through the existing Hagalaz cache access path
- **WHEN** the web UI requests that region for 3D rendering
- **THEN** the response MUST contain enough semantic data to reconstruct all four terrain planes
- **AND** the browser MUST NOT need to interpret the original terrain cache bytecode

#### Scenario: Existing gameplay map consumers remain gameplay-focused
- **GIVEN** existing consumers use the current gameplay meaning of `IMapType.TerrainData`
- **WHEN** render-specific height and floor fields are introduced
- **THEN** they MUST be exposed through an explicitly render-focused projection or another backward-compatible boundary
- **AND** existing gameplay terrain-flag semantics MUST NOT silently change

### Requirement: Rendering architecture uses semantic domain names

Hagalaz MUST use evidence-backed semantic rendering names rather than generated GameClient/decompiler identifiers in new server and web architecture.

#### Scenario: A render-focused server or web type is introduced
- **GIVEN** the GameClient source still contains generated identifiers for part of the rendering subsystem
- **WHEN** a new Hagalaz DTO, API contract, service, TypeScript type, renderer abstraction or architecture document represents that concept
- **THEN** it MUST use the canonical semantic vocabulary from `docs/3d-rendering/client-rendering-deobfuscation.md`
- **AND** it MUST NOT expose a generated `Class###`, `Entity_Sub*`, `method####` or equivalent decompiler name as the public/domain concept

#### Scenario: A client field is not understood well enough to name
- **GIVEN** a GameClient render field or behavior remains ambiguous after source tracing
- **WHEN** the bounded terrain-render contract is designed
- **THEN** the implementation MUST either keep that detail internal/unresolved or perform further deobfuscation before exposing it
- **AND** MUST NOT invent a precise public name solely to remove an obfuscated identifier

### Requirement: Region terrain preserves client-parity floor semantics

A renderable map region MUST preserve the terrain information consumed by the verified game-client floor builder: heights, terrain flags, overlay ID, underlay ID, overlay shape/path and overlay rotation for four planes.

#### Scenario: Explicit and implicit height opcodes are decoded
- **GIVEN** a terrain tile contains opcode `0` or `1`
- **WHEN** the render terrain projection decodes the tile
- **THEN** its height MUST follow the verified client semantics
- **AND** upper-plane implicit height MUST preserve the verified spacing relative to the plane below

#### Scenario: Overlay tile data is decoded
- **GIVEN** a terrain tile contains an opcode from `2` through `49`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the overlay ID MUST be preserved
- **AND** the overlay shape/path MUST be derived from `(opcode - 2) / 4`
- **AND** overlay rotation MUST use the same modulo-four behavior as the verified client decoder

#### Scenario: Terrain flag and underlay data are decoded
- **GIVEN** a terrain tile contains an opcode from `50` through `81`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the terrain flag MUST be preserved as `opcode - 49`
- **GIVEN** a terrain tile contains an opcode above `81`
- **WHEN** the render terrain projection decodes the tile
- **THEN** the underlay ID MUST be preserved as `opcode - 81`

#### Scenario: A region surface needs its far-edge vertices
- **GIVEN** a 64×64 terrain region is projected for mesh construction
- **WHEN** the render contract supplies its corner heights
- **THEN** the far X and Y edges MUST be derived deterministically from authoritative terrain data
- **AND** the browser MUST NOT create missing edge heights by repeating a last row or column placeholder

### Requirement: Floor definitions expose the semantic base-color data used by the terrain slice

The render terrain contract MUST make the proven overlay/underlay semantic color inputs required by the representative region available without exposing unexplained obfuscated fields.

#### Scenario: A tile references an overlay or underlay
- **GIVEN** the selected representative region references a decoded floor definition
- **WHEN** that tile is assembled for the terrain scene
- **THEN** the renderer MUST obtain the definition's proven base color semantics from the server-side cache projection
- **AND** MUST NOT substitute a generated placeholder color for data that is available in the cache

### Requirement: The web scene has one coordinate conversion owner

The web implementation MUST convert RuneScape legacy scene coordinates to browser-renderer coordinates in one scene-assembly boundary.

#### Scenario: A region is assembled for the browser renderer
- **GIVEN** legacy terrain data is returned by the cache service
- **WHEN** the web scene assembler creates terrain geometry
- **THEN** it MUST use a region-local scene origin
- **AND** it MUST consistently map legacy map axes and height to the selected web axes
- **AND** the initial terrain scale MUST preserve the legacy 512 scene units per tile

### Requirement: The browser renderer is independent from terrain semantics

RuneScape terrain assembly MUST remain project code independent from the selected browser graphics engine, while engine-specific lifecycle is owned by a small renderer boundary.

#### Scenario: The dedicated browser renderer is introduced
- **GIVEN** the real-region vertical slice needs a 3D rendering library
- **WHEN** that dependency is added
- **THEN** Angular page components MUST NOT own cache decoding or individual triangle construction
- **AND** engine-specific initialization, resize, render and disposal behavior MUST be isolated from the server terrain contract and coordinate semantics

#### Scenario: MapLibre remains installed for other map use cases
- **GIVEN** MapLibre may be useful for a separate geographic-style overview
- **WHEN** the RuneScape 3D terrain scene is implemented
- **THEN** the core scene MUST NOT depend on MapLibre's geographic camera/custom-layer coordinate model

### Requirement: The first web milestone renders one real terrain region

The first 3D-map vertical slice MUST render one representative cache region from authoritative terrain data before multi-region streaming, object models or advanced visual effects are required.

#### Scenario: The representative region is displayed
- **GIVEN** the representative region render contract is available
- **WHEN** the web terrain viewer loads it
- **THEN** all four planes MUST be constructible from decoded real heights
- **AND** floor overlay/underlay identity, overlay shape/rotation and available base color data MUST come from decoded cache semantics
- **AND** no generated placeholder height map MUST be required
- **AND** the viewer MUST provide diagnostic camera controls and plane visibility controls sufficient to inspect the result

### Requirement: Rendering correctness is covered below the screenshot level

Decoder, API and scene-geometry behavior MUST be verifiable with deterministic automated tests independent from GPU screenshot comparison.

#### Scenario: Terrain decoding changes
- **GIVEN** the render-focused terrain decoder is modified
- **WHEN** its focused tests run
- **THEN** fixtures MUST verify height opcodes, overlay shape/rotation, terrain flags, underlays and plane spacing

#### Scenario: Web terrain geometry changes
- **GIVEN** coordinate conversion or terrain mesh generation is modified
- **WHEN** the pure TypeScript geometry tests run
- **THEN** they MUST verify coordinate conversion, representative flat/sloped tiles, overlay rotations and edge/corner positions without requiring a browser GPU


### Requirement: Map editor scene tree

The map editor SHALL list a region's terrain and placements in a tree grouped by object shape, and SHALL let the user hide terrain, groups or single placements without reloading the region.

#### Scenario: Hide a group

- **WHEN** the user hides a group in the scene tree
- **THEN** its placements leave the viewport and its rows remain available to show again

#### Scenario: Select from the map

- **WHEN** the user selects a placement in the viewport
- **THEN** the tree opens its group and marks its row, including when the row is beyond the listed limit

### Requirement: Viewport object handles

The map editor SHALL show handles on the selected object for the active Move, Rotate or Scale tool. Handles SHALL honour placement limits: moves snap to the tile grid inside the region and to planes 0 to 3 along Z, rotation uses quarter turns around the vertical axis, and scale changes the whole object in 0.25 steps between 0.25 and 4.

#### Scenario: Drag a handle

- **WHEN** the user drags a handle
- **THEN** the object follows in the viewport without orbiting the camera
- **AND** releasing records one undoable edit

#### Scenario: Drag ends where it began

- **WHEN** a drag ends without crossing a step
- **THEN** no edit is recorded

#### Scenario: Select tool

- **WHEN** the Select or Terrain tool is active
- **THEN** no handles are shown


### Requirement: Underwater terrain layer

The map preview SHALL draw a region's underwater terrain and objects beneath its surface when the region has underwater data, and SHALL let the user hide them.

#### Scenario: Region has underwater data

- **WHEN** the anchor or neighbouring selected region has a `um` archive
- **THEN** its floors render at the surface height plus the decoded depth, and its `ul` objects render on that layer

#### Scenario: Region has no underwater data

- **WHEN** the `um` archive is absent
- **THEN** the region renders as before

#### Scenario: Hide the layer

- **WHEN** the user clears the underwater terrain option
- **THEN** the layer's floors and objects are removed without reloading the region

#### Scenario: Hide the water surface

- **WHEN** the user clears the water surface option
- **THEN** floors whose material uses a water effect (2, 4, 5, 8 or 9) are omitted from the surface planes, revealing the layer beneath, while other floors remain

### Requirement: Underwater depth fog

The map preview SHALL fade underwater floors towards the fog colour of the tile's surface overlay using `(depth - 1) * 256 / scale + bias / 255`.

#### Scenario: Saturated fog

- **WHEN** every corner of an underwater floor triangle reaches full fog
- **THEN** the triangle draws the flat fog colour without its texture

#### Scenario: No surface overlay

- **WHEN** an underwater tile has no surface overlay
- **THEN** the floor is drawn without fog


### Requirement: Overlay spreading

The map preview SHALL blend overlays authored to spread into the perimeter of neighbouring tiles according to overlay shape, rotation and priority.

#### Scenario: Neighbour spreads over an underlay

- **WHEN** a spreading overlay covers a perimeter point shared with an underlay tile
- **THEN** that underlay vertex takes the overlay's colour and texture

#### Scenario: Competing overlays

- **WHEN** several spreading overlays claim one perimeter point
- **THEN** the highest priority, then the highest definition ID, owns it, and another overlay's face uses it only if it outranks that face

#### Scenario: Non-spreading overlay

- **WHEN** an overlay does not set the spreading flag
- **THEN** neighbouring tiles are unchanged

### Requirement: Overlay render properties

The map render API SHALL expose the decoded overlay priority, spreading flag and water parameters, using client defaults when opcodes are absent, and SHALL omit them for underlays.

#### Scenario: Overlay omits the opcodes

- **WHEN** an overlay definition sets none of these opcodes
- **THEN** the response carries priority 8, no spreading, fog colour `0x122B3D`, scale 512, bias 255 and parameters 63, 0 and 64


### Requirement: Region sky and fog

The map preview SHALL use the selected anchor region's decoded sky colour as the viewport background while region atmosphere is active, and SHALL offer linear fog derived from the region's raw fog value.

#### Scenario: Region supplies sky and fog

- **WHEN** the selected anchor region contains a lighting record with sky colour or fog flags
- **THEN** the map render response includes the sky colour and raw fog value, and the viewer clears to that colour

#### Scenario: Lighting record omits sky and fog

- **WHEN** the lighting record does not set the sky colour or fog flags
- **THEN** the response carries the client defaults `0xC8C0A8` and `0`

#### Scenario: Preview fog

- **WHEN** the user enables fog while region atmosphere is active
- **THEN** geometry fades linearly to the sky colour between `14332 - ((fogValue + 256) << 2)` and `14332` scene units of eye depth

#### Scenario: Region supplies an environment map

- **WHEN** the selected anchor region's lighting record sets the environment-map flag and all six face textures load while region atmosphere is active
- **THEN** material effects 7, 8 and 9 sample a cube built from those faces in `+X, -X, +Y, -Y, +Z, -Z` order, effect 1 keeps its procedural sky, and the face images are not listed as surface textures unless a surface also uses them

#### Scenario: Environment map is unavailable

- **WHEN** the record omits the faces, a face fails to load, or default lighting is selected
- **THEN** material effects 7, 8 and 9 use the procedural sky

#### Scenario: Default lighting

- **WHEN** region atmosphere is unavailable or the user selects default lighting
- **THEN** the viewer keeps its existing background and draws no fog


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


### Requirement: Region atmosphere lighting

The map preview SHALL initially use the selected anchor region's decoded atmosphere light color, intensity, direction and response when available.

#### Scenario: Region supplies atmosphere lighting

- **WHEN** the selected anchor region contains a supported lighting record
- **THEN** the map render response includes its lighting values and the viewer applies them to the scene

#### Scenario: Region has no usable atmosphere lighting

- **WHEN** the lighting record is absent or malformed
- **THEN** renderable terrain remains available and the viewer uses its existing lighting defaults

#### Scenario: Compare lighting modes

- **WHEN** the user switches between region atmosphere and default lighting in the viewport toolbar
- **THEN** the viewer updates the shading while preserving the camera and current region selection


### Requirement: Region point light preview

The admin map editor SHALL preview cached surface point lights by default using their position, color, radius, tile footprint and plane coverage. The preview uses steady intensity and radial falloff.

#### Scenario: Lights affect loaded surface geometry

- **GIVEN** a loaded region contains a point light
- **WHEN** point lights are enabled
- **THEN** nearby terrain and objects on covered tiles and planes receive its color, including in an adjacent loaded region
- **AND** lights follow the terrain height at their source tile.

#### Scenario: Preview can be toggled independently

- **WHEN** the operator toggles Point lights in View > Lighting
- **THEN** the preview switches independently of region atmosphere and preserves the camera and selection.
- **AND** the menu shows whether point lights are enabled.

#### Scenario: Terrain data has no usable light records

- **GIVEN** a region has no light records or its terrain trailer is truncated
- **WHEN** the editor loads the region
- **THEN** the terrain remains available and the region contributes no lights.

### Requirement: Admin Maps lists available regions before opening a detail scene

The admin Maps type MUST show a paginated table of all normal regions with terrain or landscape files in the cache. Each region MUST appear once with its ID, coordinates and separate file-presence indicators. Region links and an Open map action MUST open a read-only 3D detail view within shared Cache Studio navigation. Browsing the table MUST NOT load scene data or initialize a renderer.

#### Scenario: Available maps are browsed
- **GIVEN** the administrator opens the Maps type
- **WHEN** the region table loads
- **THEN** pagination can reach every region present in the cache in ascending region-ID order
- **AND** terrain-only and landscape-only regions are included once each
- **AND** absent IDs are not shown as existing maps
- **AND** no terrain, landscape, model or texture payload is decoded or requested for list rendering

#### Scenario: A listed map is opened
- **GIVEN** a region row in the Maps table
- **WHEN** its region ID link or Open map action is activated
- **THEN** the region's detail URL opens and automatically loads its available terrain and landscape objects in one 3D scene
- **AND** the detail identifies the source region ID and coordinates
- **AND** the action is operable by keyboard

#### Scenario: The table is empty or fails to load
- **WHEN** no regions exist or the listing request fails
- **THEN** the table distinguishes empty data from a request error
- **AND** a request error offers a retry without fabricating rows or opening a detail scene

#### Scenario: A region is located by ID
- **GIVEN** a valid region ID is entered in the Maps browser
- **WHEN** the lookup is submitted
- **THEN** it opens that region's detail URL using the same navigation behavior as other type ID lookups
- **AND** an unavailable region is reported explicitly in the detail

#### Scenario: Region metadata cannot be enumerated
- **GIVEN** the cache lacks the named reference metadata needed to identify map regions
- **WHEN** the region table is requested
- **THEN** the API and UI report that listing is unavailable
- **AND** they do not mistake this for an empty cache or enumerate archive IDs as region IDs

### Requirement: Multiple regions share a bounded scene

The map detail view MUST initially load its route's region alone and MUST support a rectangular selection anchored at that region ID, with width and height each between one and three regions. Available regions MUST be placed at their real relative coordinates in one scene, with matching shared boundary heights. Invalid selections MUST be rejected before render data is requested.

#### Scenario: Adjacent regions are loaded together
- **GIVEN** an available two-by-two selection
- **WHEN** the administrator loads the selection
- **THEN** all four regions appear at offsets corresponding to their packed region coordinates
- **AND** the shared terrain-edge vertices agree
- **AND** object placements retain their region-relative positions without duplication

#### Scenario: The largest supported selection is loaded
- **WHEN** a valid three-by-three selection is applied
- **THEN** the workspace loads at most nine selected region scenes
- **AND** it identifies the nine selected region IDs

#### Scenario: A selection exceeds the supported domain
- **WHEN** the region ID is outside zero through 65535, a dimension is outside one through three, or the rectangle would extend beyond region coordinate 255
- **THEN** the workspace shows an actionable validation message
- **AND** it makes no render request for that selection
- **AND** coordinates do not wrap into another region row

### Requirement: Landscape objects use placement-aware static appearance

The region scene MUST render available ordinary static landscape objects using their decoded placement shape, orientation, footprint and definition appearance, including applicable terrain contouring. Model data MUST be reusable across placements without baking one placement's transforms into another. Unsupported appearances MUST be identified rather than replaced by invented models.

#### Scenario: A static object is placed on terrain
- **GIVEN** a decoded object placement and its supported render appearance
- **WHEN** the region scene is assembled
- **THEN** its model selection, rotation, footprint center, scale, offsets, recolors, retextures and applicable contouring follow the verified revision-742 semantics
- **AND** its model geometry and textures come from the cache
- **AND** walls and wall decorations use their placement-specific assembly rules

#### Scenario: One appearance has multiple placements
- **GIVEN** the same object appearance occurs at different rotations or terrain heights
- **WHEN** those placements are assembled
- **THEN** each appears at its own position and orientation
- **AND** assembling one does not change the source geometry or another placement

#### Scenario: An appearance cannot be rendered
- **WHEN** a model is absent, an appearance is unsupported, or an asset request fails
- **THEN** the workspace identifies the affected object and reason
- **AND** other renderable terrain and objects remain visible
- **AND** it does not substitute a placeholder model

### Requirement: Region availability is reported independently for terrain and landscape

The render API and workspace MUST distinguish absent terrain, absent landscape, landscape decode failures and request failures for each selected region. The render API accepts optional per-region XTEA keys, which MUST contain exactly four signed 32-bit integers and MUST remain outside URLs and persistent browser storage. Unavailable landscape MUST NOT prevent available terrain from rendering.

#### Scenario: Terrain exists but landscape needs keys
- **GIVEN** terrain is readable and landscape cannot be decoded without the region's keys
- **WHEN** the region is loaded without working keys
- **THEN** available terrain remains visible
- **AND** landscape is reported as unavailable
- **AND** the region is not reported as a successfully decoded empty landscape

#### Scenario: Correct keys are supplied
- **GIVEN** the render API receives four valid keys for an affected region
- **WHEN** the region is retried
- **THEN** the server decrypts the landscape container before decoding its placements
- **AND** successful placements replace the previous unavailable result without duplication
- **AND** the keys do not enter the address bar or persistent browser storage

#### Scenario: Key input is malformed
- **WHEN** supplied keys have the wrong length, fractional values or values outside signed 32-bit bounds
- **THEN** input validation identifies the affected region
- **AND** the API does not start decoding that invalid input

#### Scenario: A neighboring region or required border is absent
- **GIVEN** part of a selected rectangle or its authoritative terrain border is unavailable
- **WHEN** the scene finishes loading its available data
- **THEN** the affected region or incomplete boundary is identified
- **AND** other valid regions remain viewable
- **AND** the missing data is not silently replaced with flat terrain, repeated edge heights or generated objects

### Requirement: Corrupt region map files are reported per file

The render API MUST report a stored terrain, landscape, underwater terrain or underwater landscape file that cannot be decoded, for any corrupt-data reason, for that file with a warning, and MUST NOT fail the region request because of it.

#### Scenario: Landscape data is malformed
- **GIVEN** terrain is readable and the stored landscape decodes to a placement outside the four source planes
- **WHEN** the region is rendered
- **THEN** the region request succeeds with its terrain
- **AND** landscape is reported as failed with a warning naming the region

### Requirement: Region viewing controls remain usable across layouts

The Maps workspace MUST provide orbit, pan, zoom, reset-to-selection, independent visibility for the four terrain planes and a landscape visibility control. Controls MUST have accessible names and remain operable by keyboard and at phone widths. Loading, empty and failed states MUST include text feedback and applicable retry actions.

#### Scenario: Visibility and camera controls are used
- **GIVEN** one or more loaded regions
- **WHEN** the administrator changes plane or landscape visibility or resets the camera
- **THEN** visibility changes do not refetch unchanged region data
- **AND** resetting frames the current selection
- **AND** all actions are available without a pointing device

#### Scenario: Maps is used on a narrow viewport
- **WHEN** the workspace is displayed at phone width
- **THEN** region inputs, status and visibility controls remain reachable
- **AND** the viewport stays within the page width
- **AND** errors and retry controls are not hidden behind the scene

### Requirement: Pointer depth controls map navigation

The perspective map editor SHALL account for the depth under the pointer during wheel zoom and right-button pan.

#### Scenario: Navigate a near or far tile

- **GIVEN** the pointer intersects the map's ground reference plane
- **WHEN** the operator uses the wheel or drags with the right mouse button
- **THEN** wheel zoom scales by the same proportion at either depth, and a right drag moves the reference point with the pointer.

#### Scenario: The pointer has no usable ground depth

- **WHEN** the pointer looks away from the ground or its intersection is too far away
- **THEN** the existing camera controls handle the gesture.

### Requirement: Applied selections and scene resources have a single lifecycle

The table's search/page context, detail region ID and applied rectangle dimensions MUST be addressable in URLs and restored by refresh and browser history. Returning from detail MUST restore the table context. A superseded selection MUST NOT overwrite the current scene. Scene replacement and navigation MUST cancel obsolete work and release owned resources. Retry MUST act on the current selection without duplicate content.

#### Scenario: A direct detail link is opened
- **WHEN** a valid map detail URL is opened without first visiting the table
- **THEN** its route region ID anchors the scene and its dimensions default to one when omitted
- **AND** returning to Maps opens the region table

#### Scenario: The administrator returns to the table
- **GIVEN** a map was opened from a non-first table page with a search context
- **WHEN** shared Cache Studio navigation or browser Back is used
- **THEN** the prior table search, page size and page are restored
- **AND** the map detail releases its scene and pending asset work

#### Scenario: Selection is restored
- **GIVEN** a valid applied selection in a map detail URL
- **WHEN** the page is refreshed or reached through browser back or forward navigation
- **THEN** the route region ID and applied dimensions are restored and loaded
- **AND** local camera state and XTEA keys are not required URL parameters

#### Scenario: An older request finishes late
- **GIVEN** selection A is loading
- **WHEN** selection B is applied before A completes
- **THEN** only selection B can become the current scene
- **AND** obsolete requests and decoded image resources are cancelled or released

#### Scenario: The workspace is left and reopened
- **GIVEN** a loaded Maps scene
- **WHEN** the administrator navigates to another cache type
- **THEN** the old scene releases its render loop, observers, controls, geometries, materials, textures and decoded images
- **AND** reopening Maps creates one active scene lifecycle

### Requirement: Region scene correctness has numeric and real-cache evidence

The region viewer MUST have deterministic decoder, API and pure scene-assembly tests, plus a pinned representative real-cache comparison. Coverage MUST include terrain floors and heights, shared edges, static placement transforms and the request lifecycle. GPU screenshots MUST complement rather than replace semantic assertions.

#### Scenario: Representative terrain and landscape are verified
- **GIVEN** pinned revision-742 reference inputs with recorded cache and source provenance
- **WHEN** render projections and scene inputs are compared
- **THEN** expected heights, floors, shaped-overlay rotations, boundary coordinates and representative wall, decoration and contoured-object placements agree numerically
- **AND** the normal test run requires no live GameClient process or cross-repository network access

#### Scenario: Failure and replacement behavior is verified
- **WHEN** focused API, store and component regressions run
- **THEN** they verify complete region listing and pagination, list-to-detail/back navigation, direct detail links, malformed selections and keys, missing terrain, landscape failures, successful retry, stale response rejection and resource cleanup
- **AND** measured frontend coverage and browser verification distinguish fixture-backed checks from live-cache checks

### Requirement: Ordinary underlays blend across tile boundaries

The map viewer MUST retain decoded floor tint when textures load and MUST interpolate ordinary opaque underlay colours and textures across shared tile boundaries using the available selected terrain. Texture scales and plane separation MUST remain intact. Transparent and special-effect surfaces MUST retain their dedicated material path.

#### Scenario: Adjacent underlays have different materials
- **GIVEN** neighboring terrain tiles have different ordinary underlay textures or colours
- **WHEN** their shared edge is rendered
- **THEN** both tiles use matching normalized material contributions on that edge
- **AND** each texture retains its own world-space scale
- **AND** the transition does not depend on transparent coplanar overlay passes

#### Scenario: Texture loading finishes
- **WHEN** a terrain texture becomes available
- **THEN** the decoded grass or path tint remains applied instead of turning white

### Requirement: Landscape placement selection

The region editor SHALL let users select a rendered landscape placement in the viewport and inspect its source identity and tile.

#### Scenario: Select or clear a placement
- **WHEN** the user clicks a visible landscape placement without dragging the camera
- **THEN** the viewport highlights that placement and the inspector shows its object ID, shape, and tile
- **AND** selection clears when the user clicks terrain, clears it explicitly, or hides the selected placement

### Requirement: Local map placement editing

The map editor SHALL support preview-only changes to rendered object placements using the inspector and the loaded object catalog.

#### Scenario: Preview an object edit
- **WHEN** the user transforms, adds, or removes a placement
- **THEN** the map updates locally without writing to the cache
- **AND** discarding edits restores the source placements

### Requirement: Object catalog and placed object list

The Objects tray SHALL browse existing object definitions across the cache independently of the loaded map. The Scene inspector SHALL list rendered placements on visible planes.

#### Scenario: Place a catalog asset
- **WHEN** the user drops a supported object asset on renderable terrain
- **THEN** the editor adds a local placement at the target tile and loads its appearance through the scene loader
- **AND** the Scene inspector lists the rendered placement separately from the catalog asset
