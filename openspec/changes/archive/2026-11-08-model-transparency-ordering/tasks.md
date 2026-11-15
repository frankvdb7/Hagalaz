# Tasks

## 1. Preserve cache priorities

- [x] 1.1 Preserve effective priorities in legacy and newer `ModelGeometryDecoder` results; verify both uniform and per-face binary fixtures, including priorities 10 and 11, and document the client priority-pass reference used (AC1, AC2).
- [x] 1.2 Carry face-aligned priorities through standalone responses and item/NPC/object assembly, backend/frontend DTOs and `prepareCacheModel`; verify mixed-default multipart fixtures, corner winding, source-face correspondence and backwards compatibility without the additive field (AC1, AC3).

## 2. Order current transparent faces

- [x] 2.1 Implement priority/depth/source-face ordering in `ModelScene` using ordered submissions over unchanged source geometry; verify opaque/cutout depth writes, face-alpha and texture-mode-2 classification, priority overriding depth, equal-priority camera reversal and stable depth ties in focused renderer tests (AC2).
- [x] 2.2 Refresh ordering after camera, geometry and animated alpha changes and clear it with viewer resources; verify animated pass transitions, retained material/UV/particle associations, model replacement and disposal without per-frame resource replacement (AC3).
- [x] 2.3 Update preview warnings and model-rendering documentation to describe supported priorities and the limits of centroid sorting and client backend parity; verify warning expectations and compare a priority-sensitive fixture against the referenced GameClient pass (AC2, AC4).

## 3. Integration validation

- [x] 3.1 Run focused decoder/API/viewer tests and affected backend and both frontend builds; inspect the diff and record command outcomes and new warnings (AC1-AC4).
- [x] 3.2 Check a priority-sensitive and an ordinary overlapping transparent model in the browser through orbit and animation; record client comparison, resource/draw-call observations and evidence paths, then validate this OpenSpec change strictly (AC2-AC4).
