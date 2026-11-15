## Context

The cache service already resolves the placed shape and object variant. The revision-742 client poses the assembled object using the part's quarter-turn orientation. The model viewer already applies decoded vertex and face transforms.

## Decisions

- The appearance API supplies the model's animation IDs and placement metadata only for animated parts. The admin uses the first valid ID as a deterministic preview.
- The existing map selection loader owns bounded clip requests, shares each clip across appearances and cancels requests on replacement. Reloading the selection retries failed reads.
- The viewport owns playback. It updates vertex ranges at cache frame boundaries using the existing render scheduler. Terrain geometry and landscape placement records are outside the playback path.
- Each landscape build shares up to 16 MiB of prepared pose arrays across its animated appearances. Once the budget is spent, uncached frames reuse the most recent pose as before. Copies of one appearance project into shared scratch buffers that the viewport copies immediately.
- The View menu restores the base pose when playback is switched off. Playback starts disabled when the browser requests reduced motion.

## Alternatives Considered

Rebuilding map geometry at each frame would also rebuild unrelated object data. A server endpoint for every posed model would repeat work and transfer geometry during playback.
