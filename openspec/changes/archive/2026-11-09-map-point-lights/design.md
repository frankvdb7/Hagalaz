## Context

Revision 742 stores local lights in opcode 1 of the terrain trailer. The cache reader already traverses this trailer for atmosphere and skips light records.

## Decisions

Decode lights in the same pass as atmosphere. Keep terrain available if that pass fails. The existing region render response owns the optional data. There is no separate fetch or retry.

The editor projects lights against the source plane's current corner height. The renderer indexes four lights per tile and plane in cache order, as the client does for objects. Map materials sample that index per pixel. Vertex colors alone miss lights whose radius fits between tile corners. Three.js scene-wide point lights would also ignore the cached tile masks and plane coverage. Shared shaders apply the hardware client's destination-color blend after output color conversion and before fog. The editor keeps the existing materials, geometry buffers and animation path.

The Lighting menu owns the default-on preview toggle. Saves continue to use the existing terrain and landscape write paths.
