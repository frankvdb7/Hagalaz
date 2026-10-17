# world-and-interactions — revision 742 research data

**Status:** period-reference candidate evidence; direct matching client/cache verification remains required.

## Historical behavior and implementation dataset

Client-cache revision is the authority for object/NPC IDs, map placement, collision, orientation, animations, varbits and click options. Hagalaz has a version-specific network codec under Hagalaz.Services.GameWorld/Network/Protocol/742/ and map work in docs/3d-rendering/. Build one capture record per interaction: archive hash, region, coordinate+plane, object/NPC ID, menu option, incoming packet, target footprint, collision state, script invocation and outgoing updates. Preserve authoritative map ownership and reject stale or blocked targets. No exact coordinates or opcodes were established by this research.

## Acceptance evidence still missing

- Direct 742 client/cache ID and protocol captures with checksum and date.
- Complete content rows and the matching Hagalaz repository definitions.
- Positive, negative, interruption, concurrency and persistence tests for each interaction.

[Implementation guide](../world-and-interactions.md) · [Research policy](../research-evidence.md)
