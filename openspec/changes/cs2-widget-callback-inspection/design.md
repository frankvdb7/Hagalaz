# Design

## Context

`decodeWidgetComponent` already reads the selected widget's cache members for the preview. It returns after the component's options and does not read the callback tail. The CS2 detail resource already exposes ordered instructions, typed operands, argument and local counts, and switch tables.

## Goals / Non-Goals

Use one cache read per existing view and derive inspection data from its loaded record. See the proposal for the read-only scope.

## Decisions

- **Decode callbacks in the Cache service.** Add a guarded widget components read that returns each archive slot with its existing preview bytes and typed callbacks. Decode callback values and triggers using the revision-742 component format. Return null callback metadata for an unreadable tail so preview bytes remain available. Keep the generic archive API and the preview layout decoder.
- **Derive pseudocode in the Cache service from the decoded script.** Extend the existing script detail response with structured source rows so the admin can render the source and component links from one request. Begin with verified constants, local loads and stores, returns, direct calls with known signatures, and widget operations needed for the fixtures. Resolve branches and switches to labeled blocks where their targets are known. Track values on the int, string/object, and long stacks. Emit statements only where stack effects and input values can be derived. Mark unresolved regions with instruction-indexed raw output and resume only where the analysis can establish a valid input state. A mnemonic-only listing would still leave administrators tracing stack values to find widget operands.
- **Derive component links from opcode semantics.** Unpack a constant into widget and component IDs only when a verified opcode consumes that operand as a component pointer. Carry references as structured data through the formatter so rendered source can use router links. Add a `component` query parameter to the existing widget detail route and resolve it against the loaded hierarchy. The widget page owns selection and preview highlighting. Keep browse context through the existing navigation patterns. Scanning constants for numbers that resemble packed IDs would create misleading links.
- **Reuse request and error ownership.** The existing widget archive resource and CS2 detail store own their respective loading, cancellation, and retry states. Callback parsing and source rendering introduce no extra fetch or retry. If a link points to a missing target, use the destination's missing state and retain normal back navigation.

## Risks / Trade-offs

- **Version-dependent component tails.** Verify offsets and tagged values against an independently checked pinned-cache component. Cover truncation after preview fields with a focused regression.
- **Opcode labels and operand order.** Ground names and stack effects in local revision-742 dispatch evidence. Script 279 is a candidate fixture for a widget reference. Verify opcode 902 before naming it or use another pinned script with a proven widget operation.
- **Partial source can look complete.** Keep exact raw rows accessible and visibly mark unsupported regions. Do not derive widget links from uncertain stack values.

## Migration Plan

Deploy the Cache service before the admin update. The script detail response gains source rows and the widget components route is additive.
