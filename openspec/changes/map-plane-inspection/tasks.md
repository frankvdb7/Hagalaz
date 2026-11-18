# Tasks

## 1. Scene

- [x] 1.1 Add a shared see-through plane mask and screen-door discard to the map program wrapper without new materials or programs.
- [x] 1.2 Skip see-through planes in object, terrain, and hover picks, and list the planes under the pointer.
- [x] 1.3 Draw the map grid on the active plane only, and centre the camera on one plane on request.

## 2. Viewport and page

- [x] 2.1 Show all four planes by default. Add the active plane and "Show all planes" reset to the Layers menu, with plane slabs under the orientation gizmo that cycle visible, see-through, and hidden.
- [x] 2.2 Show each plane's state and the active plane on the plane slabs. Mark the active plane in tile details.
- [x] 2.3 Explain clicks where an upper plane covers the active plane. Add "Focus plane N" to the camera menu.
- [x] 2.4 Place dropped objects on the active plane, and leave see-through planes out of the walkability overlay.
- [x] 2.5 Load the active plane's floor textures and object appearances first, then the nearest planes.
- [x] 2.6 Hide loaded planes with a shader mask instead of a rebuild, and reduce translucent draw calls, re-sorts, index uploads and pose recomputation for scenes with all planes loaded.
- [x] 2.7 Cull map chunks outside the camera frustum without adding draw calls.

## 3. Verification

- [x] 3.1 Add scene, viewport, and page regression tests for independent focus, visibility, and see-through state; the slab cycle; picking; overlap notes; reset; and repeated toggles that do not replace geometry or materials.
- [x] 3.2 Run the admin tests, build, Biome lint, and strict OpenSpec validation. Check the viewport in a browser at desktop and phone widths.
