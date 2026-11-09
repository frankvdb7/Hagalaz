## Why

The admin map editor renders animated landscape objects in their base pose. Editors need to see the ambient motion defined by the cache in the context of the map.

## What Changes

- Play the first valid sequence on each visible object's resolved definition and provide a View menu switch.
- Keep the base pose usable while clips load and when a clip is unavailable.
- Accept when rotated and edited placements animate with cache frame timing, and selection and landscape saves continue to work.

Live game events, random sequence selection, tweening and animation authoring are out of scope.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: Add ambient animation playback to map object rendering.

## Impact

The cache appearance API exposes pose inputs. The admin uses the existing sequence and frame endpoints and pose decoder. The map renderer updates affected vertex ranges in its existing mesh.
