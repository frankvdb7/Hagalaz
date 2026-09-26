# GameClient renderer-parity fixture consumption

This document is the Hagalaz-side contract for consuming renderer-neutral reference data produced by `frankvdb7/Hagalaz.GameClient` under rendering tracker #144, especially parity workflow #151 and its implementation children #159-#165.

It supports Hagalaz issue #494 and complements `web-renderer-foundation.md`.

The key rule is simple:

> Hagalaz should consume **proven revision-742 semantics and committed reference data**, not the Java renderer architecture.

## Why this exists

The GameClient research has already established detailed behavior for:

- texture/material lookup and UV mapping;
- model normals, alpha, shading and face ordering;
- terrain packed-HSL, smoothing, shaped overlays, shadows and lighting;
- static-object model selection, transforms and terrain contouring;
- special materials/water;
- static/flickering point lights;
- environment/fog/atmosphere state.

Re-deriving those formulas independently in C# and TypeScript would create a second source of truth and make subtle revision-specific drift very likely.

The GameClient parity fixtures provide a better boundary:

```text
same revision-742 cache identity
        ↓
GameClient renderer-neutral reference state
        ↓
Hagalaz cache/render projection
        ↓
normalized semantic comparison
        ↓
Web 3D scene inputs
```

## Runtime boundary

Hagalaz must **not** depend on the GameClient at runtime.

Do not:

- start a Java client process from Hagalaz services/tests;
- call into Java through interop;
- download GameClient fixture files from GitHub at normal test runtime;
- expose Java renderer types through Hagalaz APIs;
- copy OpenGL/D3D/software renderer classes into C#/TypeScript.

The cross-repository relationship is a development/test evidence relationship only.

## Source fixture contract

The GameClient side defines:

- fixture manifest schema: `docs/rendering/parity-fixture-schema.md`;
- candidate selection: `docs/rendering/parity-fixture-candidate-discovery.md`;
- semantic reference state: `docs/rendering/parity-semantic-capture-contract.md`;
- optional visual references: `docs/rendering/parity-visual-reference-contract.md`.

Hagalaz should consume stable **semantic JSON references** first. PNG references are mainly human/optional visual evidence and are not the primary automated cross-renderer contract.

## Import strategy

Normal Hagalaz tests must not rely on live cross-repository access.

Preferred approach:

```text
Hagalaz.GameClient committed semantic fixture
        ↓ explicit maintainer import/update
Hagalaz test fixture subset
        ↓
Hagalaz parity tests
```

Store a bounded imported subset under a clear test-fixture path such as:

```text
tests/fixtures/game-client-rendering/
  provenance.json
  manifest.json
  semantic/
```

The exact path may follow existing test-project conventions.

Do not copy the entire GameClient test fixture tree when only a small subset is needed.

## Provenance

Every imported fixture set must record:

```text
source repository
source GameClient commit
source fixture schema version
source fixture IDs
revision
cache SHA-256
import/update timestamp or command version
```

Example conceptual provenance:

```json
{
  "sourceRepository": "frankvdb7/Hagalaz.GameClient",
  "sourceCommit": "...",
  "schemaVersion": 1,
  "revision": 742,
  "cacheSha256": "...",
  "fixtures": [
    "terrain-shaped-overlay-rotated"
  ]
}
```

Do not use an unpinned `main` URL as fixture identity.

## Update workflow

Provide one explicit maintainer workflow for updating imported references.

Conceptually:

```text
updateGameClientRenderingFixtures
```

The workflow should:

1. take a specific source commit/tag/path;
2. copy only the approved fixture subset;
3. validate fixture/cache/schema identity;
4. update provenance;
5. leave normal reviewable Git diffs.

Normal tests/builds must never rewrite imported references.

If the project does not want automated cross-repository copying, a documented manual copy command/process is acceptable initially. What matters is pinned provenance and repeatability.

## Comparison layers

### Layer 1 — cache/render projection

This is the strongest initial Hagalaz parity layer.

Compare values produced by Hagalaz cache/service rendering projections against GameClient semantic references.

Examples:

- terrain heights;
- underlay/overlay IDs;
- shape/rotation;
- packed-HSL preparation;
- material ID/texture scale;
- model selected IDs;
- model UVs per render corner;
- object scale/offset/orientation/contouring results;
- point-light decoded state;
- environment/fog state.

These tests should not need Three.js/WebGL.

### Layer 2 — pure Web mesh/material assembly

For TypeScript builders, compare the browser-side renderer inputs against normalized parity expectations where practical.

Examples:

- triangle topology;
- coordinate conversion;
- render-corner duplication;
- UVs;
- alpha/material flags;
- object transform placement;
- plane visibility inputs.

Do not make the TypeScript layer re-prove raw cache opcode decoding already owned by C#/cache tests.

### Layer 3 — visual comparison

Optional and later.

Three.js/browser GPU output may not be pixel-identical to the legacy GameClient even when the RuneScape-specific semantic inputs are correct.

Use GameClient visual references primarily as:

- human fidelity references;
- diagnostics for disputed appearance rules;
- optional bounded comparison where browser/backend determinism makes it meaningful.

Do not define Web 3D correctness solely by PNG equality.

## Normalization boundary

Hagalaz does not need to use the same DTO shape as GameClient reference JSON.

Tests may normalize both sides to a small common semantic representation.

Example:

```text
GameClient reference terrain state
       ↓ normalize
TerrainParityExpectation
       ↑ normalize
Hagalaz MapRenderRegion / mesh result
```

Keep normalizers test-focused. Do not introduce a production abstraction solely because the two repositories serialize differently.

## Level-2 priority

The first useful parity imports should focus on #145-#148 semantics required for a recognizable static scene.

Suggested order:

1. flat/sloped terrain heights;
2. shaped/rotated overlay ownership;
3. floor packed-HSL/material/texture-scale inputs;
4. model topology + render-corner UVs;
5. static object shape-to-model selection;
6. recolor/retexture/scale/offset/orientation;
7. terrain contouring;
8. model normal/ordinary-alpha/static-shading inputs.

These can evolve alongside/after #423 without requiring water/fog/point lights first.

## Level-3 boundary

Special material and environment fixtures from GameClient #164 should be imported only when the corresponding Hagalaz/Web milestone exists.

Keep separate coverage for:

- water/special effect parameters;
- material animation;
- point-light flicker;
- environment/fog/HDR/skybox state.

Do not make Level-3 fixture availability a prerequisite for the basic terrain viewer.

## Renderer-neutral DTO guidance

The GameClient evidence suggests Hagalaz rendering projections need to retain concepts such as:

### Terrain

```text
region/plane/tile identity
corner heights
underlay/overlay ID
shape/rotation
floor color/material inputs
texture scale
normal/shadow inputs where milestone requires them
```

### Model

```text
positions / topology
render-face or render-corner structure
UV per render corner
normal/shading input
material ID
raw/semantic alpha
ordering metadata only where required
```

One UV/normal per original source vertex is not always sufficient.

### Object

```text
placement shape/rotation
selected ordered model IDs
resolved morph definition
recolor/retexture
scale/offset/orientation
contour mode/parameter
placement-specific contour result/input
```

Do not bake placement-specific terrain contouring into a globally shared model asset.

### Material/environment

Expose semantic parameters without reproducing backend shader state.

## Failure diagnostics

Parity failures should report fixture identity and field path.

Prefer:

```text
fixture: object-contour-mode-1
field: result.vertices[2].y
expected: -96
actual: -88
source: Hagalaz.GameClient <commit>, cache <sha>
```

Do not reduce semantic comparison to one hash mismatch.

## Drift policy

When Hagalaz disagrees with a GameClient reference:

1. determine whether Hagalaz is wrong;
2. determine whether the GameClient reference was intentionally updated after stronger evidence;
3. confirm source fixture/cache/schema provenance;
4. only then update the imported fixture.

Do not automatically bless new values because production code changed.

Any intentional parity change should state which GameClient issue/doc/reference justifies the new expectation.

## Relationship to #423

#423 remains the first basic terrain-viewer vertical slice.

This parity workflow should not force #423 to implement all of #145-#150 before it can ship.

A sensible progression is:

```text
#423 basic terrain viewer
   ↓
import first Level-2 parity fixtures
   ↓
terrain appearance parity
   ↓
static objects/models
   ↓
model/material fidelity
   ↓
optional Level-3 effects
```

Use #494 to coordinate the cross-repository parity integration rather than broadening #423 indefinitely.

## Definition of done for #494

The Hagalaz-side bridge is established when:

- a bounded GameClient fixture subset is imported with pinned provenance;
- one explicit update workflow exists;
- at least one terrain reference is compared against Hagalaz's renderer-neutral projection;
- later terrain/model/object tests reuse the same fixture reader/normalization pattern;
- normal tests require no GameClient process/network;
- imported references are read-only during normal verification;
- Level-2 remains the first priority;
- GameClient implementation/backend types do not leak into production DTOs.
