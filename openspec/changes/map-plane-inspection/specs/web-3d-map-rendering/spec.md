## ADDED Requirements

### Requirement: Map asset loading follows plane visibility

The Maps detail MUST initially request appearances and textures needed by visible terrain planes and landscape. Planes 0 to 3 MUST be visible when a map opens. Floor textures and object appearances MUST be requested for the active plane first, then for planes in order of distance from it. Showing another plane or landscape MUST load its missing assets without refetching unchanged region data. Already-loaded assets MUST be reused during repeated visibility changes within the same selection.

#### Scenario: All four planes are initially visible
- **WHEN** a map opens
- **THEN** terrain and objects on planes 0 to 3 are requested and drawn together
- **AND** a plane the administrator hides before its assets start does not request its exclusive assets
- **AND** hiding landscape avoids requesting its still-unstarted exclusive assets

#### Scenario: The active plane loads first
- **GIVEN** plane 2 is active and all planes are visible
- **WHEN** a map's assets start loading
- **THEN** plane 2's floor textures and object appearances are requested before those of planes 1 and 3, and plane 0's last
- **AND** making another plane active during loading moves its unstarted requests to the front

#### Scenario: Another plane is shown during loading
- **GIVEN** a selection already has decoded region data and some completed assets
- **WHEN** the administrator shows another plane
- **THEN** missing assets for that plane join the bounded loading work
- **AND** completed region and appearance reads are not duplicated
- **AND** switching back to an already-loaded plane reuses its assets

### Requirement: Inspect one plane while all planes stay in the scene

The admin map editor SHALL keep plane visibility, see-through planes, the active plane, and the camera as independent states. Changing the active plane SHALL NOT change plane visibility, scene geometry, or the camera. The active plane SHALL set the drop plane for new objects and the plane that the map grid marks.

#### Scenario: Change the active plane

- **GIVEN** a map with planes 0 to 3 visible
- **WHEN** the administrator makes plane 2 active
- **THEN** all four planes remain visible with unchanged geometry
- **AND** the plane slabs indicate that plane 2 is active and the grid marks plane 2
- **AND** the camera does not move until the administrator chooses to focus plane 2

### Requirement: Cycle each plane through visible, see-through, and hidden

Plane slabs that do not cover the orientation gizmo SHALL show each plane's state and mark the active plane. Clicking a plane's slab SHALL move that plane from visible to see-through, from see-through to hidden, and from hidden back to visible. A see-through plane SHALL change only presentation and picking, never cache data, geometry, materials, or visibility.

#### Scenario: Click a plane's slab repeatedly

- **GIVEN** plane 2 is visible
- **WHEN** the administrator clicks plane 2's slab three times
- **THEN** plane 2 becomes see-through, then hidden, then visible and opaque again
- **AND** the active plane and the other planes are unchanged
- **AND** while plane 2 is see-through, the walkability overlay leaves it out
- **AND** repeated clicks reuse the existing geometry, materials, and shader programs
- **AND** underwater terrain is never drawn see-through

#### Scenario: Restore the combined view

- **GIVEN** some planes are hidden or see-through
- **WHEN** the administrator chooses "Show all planes"
- **THEN** all four planes are visible and opaque
- **AND** the active plane and camera are unchanged

### Requirement: Select lower floors behind upper-floor geometry

Picks SHALL pass through see-through planes. Object selection, terrain tile selection, and hover SHALL then reach the floors below. When a click selects geometry on an opaque plane that covers the active plane, the viewport SHALL explain the overlap and direct the administrator to make that plane see-through.

#### Scenario: Select an object under a roof

- **GIVEN** a plane 2 roof covers a plane 0 object
- **WHEN** the administrator makes plane 2 see-through and clicks the object through the roof
- **THEN** the plane 0 object is selected

#### Scenario: Click an opaque roof

- **GIVEN** plane 0 is active, plane 2 is opaque, and a plane 2 roof covers plane 0 under the pointer
- **WHEN** the administrator clicks there
- **THEN** the viewport states that plane 2 covers plane 0 at that point and how to select through it

### Requirement: All-plane scenes stay interactive

With every plane loaded, showing, hiding, or making a loaded plane see-through SHALL NOT rebuild the scene geometry. Translucent faces SHALL be drawn back to front in camera-distance slabs grouped by material, and camera pans, object animation ticks, and small turns SHALL NOT re-sort them.

#### Scenario: Toggle a loaded plane

- **GIVEN** all four planes of a large region are loaded
- **WHEN** the administrator hides plane 2 and shows it again
- **THEN** the scene geometry is reused, objects on plane 2 cannot be picked while it is hidden, and its animations resume when it is shown

#### Scenario: Orbit a large region

- **GIVEN** a region whose objects contain many translucent faces
- **WHEN** the administrator orbits the camera
- **THEN** translucent faces are drawn far to near with one draw call per material in each slab
- **AND** re-sorting uploads only the translucent part of the draw index

#### Scenario: Part of a large selection is off screen

- **GIVEN** a map surface spans several 16-tile chunks
- **WHEN** some chunks are outside the camera frustum
- **THEN** their translucent faces are not drawn
- **AND** their opaque ranges are skipped unless skipping them would split a material into more draw calls

## REMOVED Requirements

### Requirement: Map asset loading follows current visibility

**Reason**: Its "only the ground plane is initially visible" scenario conflicts with drawing all four planes by default.

**Migration**: Replaced by "Map asset loading follows plane visibility", which keeps the visibility-driven, non-duplicating loading behaviour with all four planes visible at open.
