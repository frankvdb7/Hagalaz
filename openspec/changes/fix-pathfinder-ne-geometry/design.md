## Context

`PathfinderBase.CheckStep` is the existing owner of creature-footprint collision validation. For a north-east unit step, a size-2 creature exposes two top-edge cells and the new right-side cell; the client reference uses `(fromX + 2, fromY + 1)` for that final cell. For size greater than two, the client geometry checks the incoming top edge with the south-facing variable composite and the incoming right edge with the west-facing variable composite.

The existing `CollisionFlag` composites are the authoritative directional masks. `WallSouthEast` is present in `CheckSouthVariable` but not `CheckWestVariable`; `WallNorthEast` is present in `CheckWestVariable` but not `CheckSouthVariable`. These bits provide focused tests for the mapping without introducing new flags or relying on shared `FloorBlock` bits.

## Decisions

### Keep `PathfinderBase` as the single validation owner

The two coordinate/mask corrections stay in the existing shared validator. Changing `Movement` or individual pathfinder consumers would duplicate the footprint rules and would not protect other callers of `CheckStep`.

### Use client footprint geometry as the reference

The size-2 north-east checks remain three exposed cells, with only the third coordinate corrected. The variable-size north-east loop retains its current coordinates and bounds while swapping only the two directional composites to match the top/right edge orientation.

### Test directional masks with exclusive bits

The regression suite will use `WallSouthEast` on an incoming top-edge cell and `WallNorthEast` on an incoming right-edge cell for sizes 3 and 4. Each bit is exclusive to the expected composite, so a swapped mask fails the test rather than being hidden by a common floor bit.

## Rejected Alternatives

- Refactoring all diagonal branches: unnecessary for two localized errors and increases regression risk.
- Adding special-case checks in `Movement`: the root behavior belongs to the shared validator and would leave pathfinding callers inconsistent.
- Using `FloorBlock` for mask tests: it is included in every directional composite and cannot distinguish the required mapping.
