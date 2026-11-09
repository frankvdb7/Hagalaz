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

Opcodes 2, 17, 25 and 29 have zero use in this cache. Skip their simulation ports. Region selection for timing remains open until the tweening phase.

## Phase 1 client trace

`Model.method4454` dispatches the unmasked transform through `method4470` and uses a separate path for masked transforms. The software renderer's `Model_Sub3` builds a Class20 and a mutable Class10 for each Class94 attachment, using a Class207 definition from archive 0. The draw path places the attachment at the posed triangle's integer centroid, projects its offsets and dimensions, and calls `method5201` with its texture and face-derived ARGB. Class10 holds the pose offsets, scales and rotation. Type 7 changes the face HSL and updates the attachment color. Types 8, 9 and 10 change offsets, a wrapped 14-bit angle and scale respectively. The software sprite draw call omits the angle. OpenGL uses the angle in Model_Sub2 via Class40 and passes it to the camera-space quad transform near line 3851. The OpenGL path also uses fixed-point dimension scaling and a depth offset toward the camera. Match that hardware draw path for WebGL.



Type 7 now passes posed HSL through the same lighting path as the base model in both preview and map assembly. A temporary Java program invoked the compiled client's `Model.method4454` on `Model_Sub3`, yielding 2047, 63488 and 128 for the test faces. The port's packed HSL masks and shifts were compared programmatically with `method4470`. The port follows the OpenGL models, `Model_Sub1` and `Model_Sub2`, which change saturation by the delta divided by 4 with integer truncation. The software `Model_Sub3` adds the delta directly. `Model.method4453` applies a transform's pivot before its type dispatch, so transforms that move no vertices still set the pivot. Unhandled transform types produce a warning and the supported transforms still run. The preview checks every frame of a clip, as the map does, so the warning stays steady during playback.

Phase 1 remains open. Types 8–10 require the Class94 definition and geometry data, face-derived color and alpha, and the camera-facing quad draw path. Hagalaz currently exposes none of that data and renders no such quads. A pose-only port would leave their visible behavior missing. Keep the warnings until the attachment rendering is verified. The phase gate leaves phases 2–6 pending except for the effect 3 zero-use finding. The user confirmed the live admin check for object 547 at frame 201195526 and supplied base and posed captures. The bright geometry above the coil in the base pose is absent in the posed capture. Console and shader checks rely on user confirmation. The client side-by-side check remains open.

## Verification of the current patch

The shared viewer passed 147 tests. Admin passed 524 tests with coverage and the public app passed 31 tests. Cache and Cache Services passed 525 and 256 tests using isolated build directories. The admin build, Biome and strict OpenSpec validation passed. Compared with HEAD, Fallow reports no newly flagged functions. `poseModel` was already flagged and its cyclomatic complexity rose from 18 to 19 while its cognitive complexity fell from 42 to 40. Biome reported two existing non-null assertion warnings and Angular test compilation reported the existing missing `src/polyfills.ts` warning.

Shared viewer coverage for `model-pose.ts` was 92.04% statements, 87.23% branches, 95% functions and 100% lines. The admin coverage run exercised the preview, playback controls and map buffers. It did not exercise every error path or every preview template branch. Coverage does not include the unimplemented Class94 rendering path.
