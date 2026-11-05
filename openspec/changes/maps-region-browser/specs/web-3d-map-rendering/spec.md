# Spec delta

## ADDED Requirements

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

The render API and workspace MUST distinguish absent terrain, absent landscape, landscape decode failures and request failures for each selected region. Optional per-region XTEA keys MUST contain exactly four signed 32-bit integers and MUST remain outside URLs and persistent browser storage. Unavailable landscape MUST NOT prevent available terrain from rendering.

#### Scenario: Terrain exists but landscape needs keys
- **GIVEN** terrain is readable and landscape cannot be decoded without the region's keys
- **WHEN** the region is loaded without working keys
- **THEN** available terrain remains visible
- **AND** landscape is reported as unavailable with guidance to supply or check the keys
- **AND** the region is not reported as a successfully decoded empty landscape

#### Scenario: Correct keys are supplied
- **GIVEN** the administrator supplies four valid keys for an affected selected region
- **WHEN** the region is retried
- **THEN** the server decrypts the landscape container before decoding its placements
- **AND** successful placements replace the previous unavailable result without duplication
- **AND** the keys do not enter the address bar or persistent browser storage

#### Scenario: Key input is malformed
- **WHEN** supplied keys have the wrong length, fractional values or values outside signed 32-bit bounds
- **THEN** input validation identifies the affected region
- **AND** neither the UI nor direct API request starts decoding that invalid input

#### Scenario: A neighboring region or required border is absent
- **GIVEN** part of a selected rectangle or its authoritative terrain border is unavailable
- **WHEN** the scene finishes loading its available data
- **THEN** the affected region or incomplete boundary is identified
- **AND** other valid regions remain viewable
- **AND** the missing data is not silently replaced with flat terrain, repeated edge heights or generated objects

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

### Requirement: Applied selections and scene resources have a single lifecycle

The table's search/page context, detail region ID and applied rectangle dimensions MUST be addressable in URLs and restored by refresh and browser history. Returning from detail MUST restore the table context. A superseded selection MUST NOT overwrite the current scene. Scene replacement and navigation MUST cancel obsolete work and release owned resources. Retry MUST act on the current selection without duplicate content.

#### Scenario: A direct detail link is opened
- **WHEN** a valid map detail URL is opened without first visiting the table
- **THEN** its route region ID anchors the scene and its dimensions default to one when omitted
- **AND** returning to Maps opens the region table

#### Scenario: The administrator returns to the table
- **GIVEN** a map was opened from a non-first table page with a search context
- **WHEN** Back to Maps or browser Back is used
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
