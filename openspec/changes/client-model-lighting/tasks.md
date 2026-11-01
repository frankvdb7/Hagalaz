# Tasks

## 1. Preserve shading inputs

- [x] 1.1 Preserve legacy and newer face shading types through decode, multipart assembly and DTOs; verify binary fixtures with smooth/flat/suppressed faces and unchanged texture/priority/particle correspondence (AC1).
- [x] 1.2 Project raw/default and item/NPC/object effective ambient and contrast from their actual client construction paths; verify non-default endpoint fixtures, signed opcode semantics and exactly-once contrast scaling, and document the selected inspection preset (AC1).

## 2. Compute client-compatible lighting

- [x] 2.1 Implement renderer-neutral integer normals and smooth/flat per-corner lighting before expansion; verify source-derived positive/negative response, packed-colour/gamma, textured intensity clamp and alpha-sentinel fixtures and record their client source pointers (AC2).
- [x] 2.2 Integrate prepared colours into ModelScene without double lighting, preserving explicit unlit effects and procedural material effects; verify ordinary/textured material output and shader compilation, current-pose relighting, transparency, UV and particle associations (AC2, AC3).
- [x] 2.3 Replace unsupported-lighting notes with accurate isolated-lighting behavior and remaining atmosphere/backend/seam limits; verify preview warning assertions and document fixture results (AC3, AC4).

## 3. Validate and run

- [x] 3.1 Run focused decoder/API/viewer regressions and affected backend and both frontend builds; review the diff and record existing/new warnings and command outcomes (AC1-AC4).
- [ ] 3.2 Verify model #666 plus smooth/flat and animated fixtures in the browser, record before/after and numeric comparison evidence, and validate this change strictly (AC2-AC4).
- [x] 3.3 Apply the validated build to the running Aspire cache resource and admin server, verifying both healthy and ready for a user reload (AC4).
