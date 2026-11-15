# Proposal

## Why
Completionist cape 20769 has model-attached particles that the viewer currently drops. Administrators need the same attachment-driven preview for other cached models.

## What Changes
Decode model emitter attachments and particle definitions, preserve attachment indices through assembly and posing, and render bounded textured particle previews in the shared viewer's existing animation loop. Load particle textures through existing protected assets. Report unverified forces/collision and unsupported emitter fields visibly.

## Acceptance criteria
1. Real item20769 fixtures expose seven emitter403 face attachments on each worn model, and none on its inventory model. Assembly preserves indices and IDs.
2. Emitter settings retain source colors, texture, lifetime, rate, size and speed; protected APIs report unavailable/unsupported definitions explicitly.
3. Particles emit from selected/posed faces, animate without another loop, use source texture/color and stop/dispose on replacement or destruction. The UI labels unverified client simulation behavior and missing assets.
4. Focused backend/shared/admin tests and both production builds pass; running cache is rebuilt. Browser checks remain waived.

## Non-goals
No full world particle engine, terrain collision, external force-field simulation, full player body or invented cape-specific colors. No separate clock, worker, dependency or authentication boundary.

## Capabilities
### New Capabilities
- `model-particles`: model-attached particle inspection.
### Modified Capabilities
None.

## Impact
Cache geometry and particle decoder, existing model API projections, shared particle renderer and admin assets. Reuse current assembly, protected cache access, textures and render loop.
