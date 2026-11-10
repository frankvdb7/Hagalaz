# Design

## Context

The source of truth is `../Workspace/Hagalaz.GameClient/src/main/java`. Use the compiled classes in its `build/classes/java/main` directory for numeric fixtures where practical. The service's configured cache resolves to `Source/Cache` from its project directory.

## Goals / Non-Goals

Match model behavior in the viewer and map for features exercised by real assets. Preserve existing coordinate scale, high-precision mode, effects 1, 2 and 4–9, terrain and water. Editing and saving assets are outside this change.

## Decisions

1. Inventory cache references with the production readers before implementation. Count unique animations by sequence ID and unique skeletons by skeleton ID. Resolve packed frame references through the frame archive, then read the skeleton ID from the frame header. Count a sequence once per transform type. Record missing or failed reads separately so they cannot masquerade as zero use. Add attachment metadata to the production model decoder where a record is currently discarded or omitted.
2. `model-pose.ts` owns client-unit transforms and interpolation. Type 7 changes packed HSL through face skins. `model-lighting.ts` lights the posed color through the existing lighting path. `ModelDefinition` reads Class94 records after the particle block and `Model_Sub3` constructs Class10 records from them. Trace the actual draw path and group transforms before choosing which fields to expose.
3. Both consumers use the same pose sampling. Preserve stepped playback for non-tweened sequences. `ModelScene.renderObjectAnimations` owns the map update cadence and `writePlacedAnimationPose` writes placement buffers. Measure elapsed frame work on the heaviest test region before accepting the interpolation cost. A second render loop or independent pose clock would permit the preview and map to diverge.
4. Decode effector IDs and attachment indices in the cache layer. Carry attachments through assembly and the dedicated DTOs. Verify how the client resolves the attachment index in the posed model and applies forces. Keep simulation and bounded resource ownership in `model-particles.ts`. Keep unsupported opcode warnings until the corresponding behavior is verified.
5. The placed appearance path already mirrors Z and reverses triangle winding and associated UVs. Reuse that geometry operation for standalone object appearance and image generation, preserving placement-specific rotation and computing lighting from the mirrored geometry.
6. Trace effect 3 through `Class48` and `Class49_Sub2`. If the client needs scene inputs unavailable to model previews, retain base appearance and name the missing inputs. If portable, keep parameters and GLSL in `material-effects.ts` and reuse the scene's existing resource lifecycle.

## Risks / Trade-offs

An approximate transform can change unrelated geometry or hide a parity gap. If a phase cannot match the client, record the asset, client path and specific blocker here and keep the limitation visible. A zero count permits skipping the corresponding feature only when the relevant archive reads are accounted for.

## Phase 0 inventory

Scanned the local revision-742 cache at `Source/Cache`. A temporary console program used the production cache API, type providers and decoders. The model decoder copy added two count reads at the existing attachment offset for effectors and Class94. The scan read 3,669 skeletons, 17,962 sequences and their referenced frames, 86,104 models, 900 emitter definitions, 2,738 materials and 78,753 object definitions. No decode failures were reported by the scans.

| Transform | Skeletons | Sequences referencing a skeleton with the type | Sequences with an active transform | Candidate object |
| --- | ---: | ---: | ---: | ---: |
| 7 | 250 | 6,962 | 561 | 547 |
| 8 | 16 | 71 | 26 | 53282 |
| 9 | 147 | 799 | 305 | 2090 |
| 10 | 257 | 7,119 | 813 | 53282 |

Object 547 uses model 51769 and sequence 12783. Frame 201195526 changes face group 37, which exists in that model. Object 53282 uses model 58631 with two Class94 records and sequence 14336. Frame 217841699 targets attachment group 0 with types 8 and 10. Object 2090 uses model 65252 with three Class94 records and sequence 2234. Frame 253034530 targets attachment groups 0 and 1 with type 9. Live attachment behavior remains unverified.

| Feature | Count | Candidate asset |
| --- | ---: | --- |
| Tweened sequence | 9,093 | Object 295 has a tweened sequence |
| Model with emitter | 1,253 | Model 5 |
| Model with effector | 6 | Model 51222 |
| Model with Class94 attachment | 1,648 | Model 5 |
| Effect 3 material | 0 | Skip |
| Floor or model referencing an effect 3 material | 0 | Skip |
| Inverted object | 7,341 | Object 22 |

| Unsupported emitter opcode | Definitions | Example model referencing a definition |
| ---: | ---: | ---: |
| 18 | 900 | 60930 |
| 20 | 900 | 60930 |
| 21 | 900 | 60930 |
| 14 | 898 | 60930 |
| 16 | 897 | 60930 |
| 26 | 854 | 60930 |
| 27 | 826 | 60930 |
| 28 | 811 | 60930 |
| 12 | 757 | 60930 |
| 30 | 696 | 60930 |
| 22 | 656 | 60930 |
| 23 | 656 | 60930 |
| 10 | 365 | 60930 |
| 4 | 243 | 51222 |
| 19 | 236 | 51286 |
| 34 | 181 | 27831 |
| 32 | 128 | 51286 |
| 24 | 106 | 60930 |
| 13 | 60 | 29942 |
| 33 | 54 | 10333 |
| 9 | 23 | 51222 |
| 2 | 0 | — |
| 17 | 0 | — |
| 25 | 0 | — |
| 29 | 0 | — |

Opcodes 2, 17, 25 and 29 have zero use in this cache. Skip their simulation ports. Region 12850 was selected for tweening measurements.

## Phase 1 client trace

`Model.method4454` dispatches the unmasked transform through `method4470` and uses a separate path for masked transforms. The software renderer's `Model_Sub3` builds a Class20 and a mutable Class10 for each Class94 attachment, using a Class207 definition from archive 0. The draw path places the attachment at the posed triangle's integer centroid, projects its offsets and dimensions, and calls `method5201` with its texture and face-derived ARGB. Class10 holds the pose offsets, scales and rotation. Type 7 changes the face HSL and updates the attachment color. Types 8, 9 and 10 change offsets, a wrapped 14-bit angle and scale respectively. The software sprite draw call omits the angle. OpenGL uses the angle in Model_Sub2 via Class40 and passes it to the camera-space quad transform near line 3851. The OpenGL path also uses fixed-point dimension scaling and a depth offset toward the camera. Match that hardware draw path for WebGL.



Type 7 now passes posed HSL through the same lighting path as the base model in both preview and map assembly. A temporary Java program invoked the compiled client's `Model.method4454` on `Model_Sub3`, yielding 2047, 63488 and 128 for the test faces. The port's packed HSL masks and shifts were compared programmatically with `method4470`. The port follows the OpenGL models, `Model_Sub1` and `Model_Sub2`, which change saturation by the delta divided by 4 with integer truncation. The software `Model_Sub3` adds the delta directly. `Model.method4453` applies a transform's pivot before its type dispatch, so transforms that move no vertices still set the pivot. Unhandled transform types produce a warning and the supported transforms still run. The preview checks every frame of a clip, as the map does, so the warning stays steady during playback.

Types 8–10 now use Class94 records and Class207 definitions from index 29, archive 0. The Cache API remaps attachment faces when joining model parts. Preview and map both use the OpenGL Class40 transform arithmetic and pass the resulting state to camera-facing quads attached to the current face vertices. Quads inherit the posed face color and opacity. The definition controls the texture, blend mode and source-face suppression. The map copies updated billboard state alongside the animated placement buffers. Billboard resources follow the primary scene geometry and share the existing texture lifecycle. Unknown transforms and unavailable definitions still produce warnings. The user confirmed the live admin check for object 547 at frame 201195526 and supplied base and posed captures. The bright geometry above the coil in the base pose is absent in the posed capture. Console and shader checks rely on user confirmation. The client side-by-side check remains open.

## Verification of the current patch

The shared viewer passed 147 tests. Admin passed 524 tests with coverage and the public app passed 31 tests. Cache and Cache Services passed 525 and 256 tests using isolated build directories. The admin build, Biome and strict OpenSpec validation passed. Compared with HEAD, Fallow reports no newly flagged functions. `poseModel` was already flagged and its cyclomatic complexity rose from 18 to 19 while its cognitive complexity fell from 42 to 40. Biome reported two existing non-null assertion warnings and Angular test compilation reported the existing missing `src/polyfills.ts` warning.

Shared viewer coverage for `model-pose.ts` was 92.04% statements, 87.23% branches, 95% functions and 100% lines. The admin coverage run exercised the preview, playback controls and map buffers. It did not exercise every error path or every preview template branch. Coverage does not include the unimplemented Class94 rendering path.

### Billboard verification

The compiled OpenGL Model_Sub2 returned offsets -11 and 19, angle 4, and scale values 33 and 101 for the grouped pose regression. The port compares the wrapped angle and fixed-point scale constants with the client source. The production decoders resolved texture 849 and 847 on model 58631 and texture 1167 on model 65252. The map regression checks the second placement's remapped face, and the renderer test checks quad position, size, angle, source-face suppression, and disposal.

Shared viewer tests passed 149, admin tests passed 524, and public tests passed 31. Cache and Cache Services passed 530 and 256. The admin build, Biome and strict OpenSpec validation passed. The final Fallow audit against HEAD reported no introduced findings. Shared billboard renderer coverage measured 100% lines and 57.14% branches. Other blend modes and missing-texture paths have less coverage than the textured alpha case. The existing Biome and Angular test warnings remain.

The user confirmed the live admin checks for object 53282 at frame 217841699 (translation and scaling) and object 2090 at frame 253034530 (rotation). Client draw ordering and its special render pass have not been compared with the WebGL output. The client side-by-side task stays open. The effect 3 zero-use finding remains recorded above.

## Phase 2 tweening

Both viewers merge sparse frame transforms using Model.method4453. Types 3 and 10 default to scale 128 when a slot is absent. The outgoing flag 2 and incoming flag 1 hold the current transform. Types 2 and 9 wrap in 14-bit angle space, and type 7 wraps hue in 6-bit space. Other axes interpolate with Java integer truncation. A missing or incompatible next skeleton keeps the current pose. The final tween tick uses the current frame duration as its denominator. The next client tick advances the frame and starts at tick one. The viewers keep their existing repeat policy. Manual frame inspection and non-tweened playback continue to show discrete poses.

The map uses its existing render callback, updates tweened placements at 20 ms ticks, and shares prepared poses within the existing pose cache budget. Repeated poses within a tick reuse the last sampled result. To reduce CPU work, posing indexes vertex skins once per pose, face lighting avoids temporary vectors, and the fixed-gamma palette reuses computed entries. Existing geometry, lighting and animation regressions exercise these paths. A Java harness ran the compiled Model.method4453 for the sparse, wrapped and held cases used in the TypeScript regression. A separate source comparison checked both angle periods and the two hold masks.

Region 12850 had the most tweened placements of the available sample regions. It had 28 among 5,934 placements, versus 0 in 10138 and 2 in 15177. Region 6960 had no available landscape in this cache. The benchmark loaded all four planes of 12850 through the cache decoders, used the production object appearance and animation readers, and measured 600 invocations of ModelScene.renderObjectAnimations on 28 animated ranges with 50,151 vertices. Each invocation advanced the clock by 20 ms. The stepped comparison used the same geometry and clips with tweening disabled.

| CPU time per update | First second median | First second p95 | Later median | Later p95 | Later max |
| --- | ---: | ---: | ---: | ---: | ---: |
| Stepped | 4.65 ms | 16.82 ms | 1.58 ms | 7.77 ms | 15.27 ms |
| Tweened | 10.21 ms | 11.92 ms | 9.83 ms | 11.78 ms | 27.35 ms |

This measures the production animation update through writes to Three.js buffers. It excludes browser rendering, GPU upload and draw time. The user confirmed tweening worked in the live admin checks for object 295, sequence 453, and region 12850. The client side-by-side check remains open.

Phase 2 verification passed 151 shared viewer, 525 admin and 31 public tests, the admin build, Biome and strict OpenSpec validation. Fallow reported no introduced issues against HEAD. Shared tween coverage measured 90.32% lines and 79.54% branches before the final equivalent face-color loop extraction. The admin suite also exercised timing and pause/resume.

## Phase 3 particle effectors

Class64 stores a definition ID and a vertex index. Model_Sub2 reads the posed position of that vertex, and Node_Sub21 uses the model transform to orient the force vector. The decoder keeps attachments in source order and reads Class208 definitions from index 27, archive 1. Assembly remaps vertices across model parts. Emitter opcode 9 selects the local effector IDs. Its decoder test reads a following texture opcode and rejects a truncated ID list.

The viewer reads current anchors before each particle update and checks the client cone and distance limits. It applies directional falloff or a signed radial force. Class208 distinguishes forces that bend the short direction vector from those that displace the 12-bit fixed-point position directly. The viewer retains those units and uses the existing particle clock and allocation limits. The model workspace applies its preview transform to anchors and rotates force vectors. The existing emitter cone and birth simulation remains an approximation. Scene-wide and constant force references remain under their existing unsupported emitter opcodes. Map objects still do not run a particle simulation.

The six attachment-bearing models use local scope. Model 51222 links emitter 9 to directional effector 1 at vertices 7, 10 and 4. Models 57543 and 69079 link emitter 394 to reverse radial effector 17. Model 76652 contains effector 10, but its emitter 316 does not select it. These are production cache reads. Use model 51222 for the live directional check and model 57543 for the live radial check.

The compiled client particle update returned raw displacements of 327680 for direct directional, -327680 for reverse radial, and 800 for the bend case over 20 ms. The renderer regressions check these values in display units and verify that animating only the attachment vertex changes which live particles receive the force. The cache service regression checks sparse definition lookup and vertex remapping across assembled parts.

Verification passed 532 cache, 258 cache service, 156 shared viewer, 525 admin and 31 public tests, the admin build, Biome and strict OpenSpec validation. Particle simulation coverage measured 100% lines and 90.47% branches. Fallow found no introduced issues with focused coverage against HEAD. The full shared coverage report also surfaced an attribution on the unchanged map navigation callback. Live browser and client side-by-side checks remain open.

A live report found no visible inward motion on the radial sample. The cache gives emitter 394 a 40–45 ms lifetime. At a 50 ms render interval the preview dropped every prior particle before applying its force, then drew every new particle at age zero. The regression uses face 2866 and the effector anchor from model 57543. Before the fix its nearest active particle stayed 1,373 units from the anchor. The viewer now advances births linked to local effectors at the subframe ages used by Class406, so one render update can show both newly spawned particles and particles already pulled inward. The test checks both parts of the spread. The live visual recheck remains open.

The directional sample model 51222 uses emitters 7, 8 and 9. Their cache definitions all specify opcode 4 mode 2 with signed strength 6. The preview now applies the client's speed damping from the current emitter triangle centroid before the local force update, and staggers births for emitters with drag. The existing per-frame spawn and draw path remains bounded. Tests cover both distance formulas and verify that the signed-byte payload leaves the next opcode aligned. This ports opcode 4 only. Other emitter settings and live comparison remain open.
