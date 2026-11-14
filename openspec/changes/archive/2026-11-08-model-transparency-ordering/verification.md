# Transparency ordering evidence

## Supported behavior

The preview preserves effective cache face priorities, submits opaque/cutout surfaces with depth writes, and orders blended surfaces by ascending priority, back-to-front triangle-centroid depth within a priority, and source face index for ties. Priorities 10 and 11 are ordinary ascending priorities in this contract. Geometry indices, material/UV associations and particle source face IDs remain unchanged.

This matches the priority precedence of the explicit transparent software pass in the sibling GameClient `src/main/java/Model_Sub3.java`, method4542, lines 4374-4392. That pass loops priorities 0..11. This is a source-derived fixture comparison, not a claim of complete GameClient visual parity. Constructor material-key sorting, billboards, intersecting triangles and client lighting remain outside the supported contract.

## Verification on 2026-11-01

- Decoder tests: 36 passed, including `Decode_PreservesUniformAndPerFacePriorities` across legacy/newer and uniform/explicit formats.
- Shared viewer tests: 41 passed. New cases cover priority precedence including 10/11, opposite camera depth order and stable ties, alpha pass transitions without replacing GPU geometry, cutout versus texture-mode-2 blending, and unchanged source topology and winding conversion.
- API model appearance tests: 10 passed from a separate artifacts directory; this also built the affected backend service. Mixed legacy/newer parts retain priorities [10,11].
- Admin tests: 241 passed.
- Admin and main frontend production builds: passed.
- Browser fixture uses the production ModelScene and actual WebGL rendering. Source face priorities [11,0,10] produced [1,2,0], matching the client explicit priority-loop result. Turning priorities off and reversing the camera changed depth ordering; a geometry frame update retained one GPU geometry. Three source faces required two draw calls in the initial coalesced order and three in reversed order.
- Browser artifact directory: `C:/Users/fmavd/.codex/visualizations/2026/11/01/01a19942-51c9-7643-9b30-500cff69a4ee/`. Includes `ordering-preview.ts`, `.html`, bundled `.js`, and `ordering-priorities.png`.
- Browser evidence uses synthetic overlapping faces, not a live authenticated admin/cache session or a running GameClient. A favicon 404 was the only browser console error.

## Warnings

The admin test build still reports the existing src/polyfills.ts compilation warning. Backend restore reports existing ImageSharp advisory warnings and package-pruning warnings. An initial API-test build hit a running cache-service file lock; a separate artifacts directory avoids changing or stopping that service.
