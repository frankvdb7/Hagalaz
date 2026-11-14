# Tasks

Work in order. Verify each phase before advancing. Record verified zero usage and client parity blockers in design.md.

## 0. Cache inventory

- [x] 0.1 Scan the production cache and record all requested counts, scan failures and real asset IDs in design.md.

## 1. Transform types

- [x] 1.1 Trace the active client transform and draw paths. Port used types 7–10 with client rounding, packed HSL relighting, and verified Class94 attachment data where needed. Warn on skipped transforms when the client continues playback.
- [x] 1.2 Verify client-derived transform values and relighting with focused tests and real assets.

## 2. Tweening

- [x] 2.1 Share client interpolation, flags and delay semantics between preview and map, preserving stepped playback.
- [x] 2.2 Verify numeric tween values, flag cases and map buffer writes. Measure the existing render path on the heaviest test region.

## 3. Effectors

- [x] 3.1 Preserve effector data through decoding, assembly and DTOs. Apply verified client forces using posed attachments.
- [x] 3.2 Verify decode alignment, attachment remapping and animated effectors.

## 4. Emitter settings

- [ ] 4.1 Port unsupported opcodes in measured usage order. Verify decoder lengths and client behavior before removing each warning. Verify bounded resources and cleanup.

## 5. Inverted objects

- [ ] 5.1 Share mirroring across map appearance, model previews and images. Verify endpoint geometry, winding and image lighting.

## 6. Effect 3

- [ ] 6.1 Trace client effect 3. Verify a portable port or a named fallback for unavailable scene inputs.

## 7. Verification

- [ ] 7.1 Run the focused cache and service tests. Isolate build artifacts if Aspire holds binaries open.
- [ ] 7.2 Run admin:test, the public test target, shared model-viewer Vitest, lint:biome, admin:build, coverage and fallow:audit. Compare Fallow findings against HEAD and fix additions.
- [ ] 7.3 Validate this change strictly. Record live admin results for each asset, including console and shader linking, and any verification gaps.
- [ ] 7.4 Compare the admin and revision-742 client side by side. Keep this task open and leave the change unarchived.
