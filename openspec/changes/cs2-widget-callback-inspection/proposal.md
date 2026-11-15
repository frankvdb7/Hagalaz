# Proposal

## Why

Cache Studio shows widget components and CS2 disassembly separately. An administrator inspecting a component needs to see which cached events call scripts and read what those scripts do.

## What Changes

- Show the selected component's cached callback events, ordered typed arguments, and associated trigger filters. Link numeric callback targets to the existing CS2 detail route.
- Add a read-only RuneScript-style pseudocode view to CS2 details. Link verified widget references to the corresponding widget and component. Keep the disassembly available and identify regions the source view cannot reconstruct.
- Have the Cache service decode widget callbacks and reconstruct CS2 source. Reuse the Cache API authorization policy and existing admin routes.

## Capabilities

### New Capabilities

- `admin-widget-callback-inspection` covers reading widget callbacks and following references between widgets and CS2 source.

### Modified Capabilities

None.

## Impact

Angular Admin Cache Studio and the Cache service. Builds on the `widget-browser` and `cs2-script-browser` changes and the local revision-742 client references.

## Acceptance and stop conditions

Verify a pinned-cache widget callback from component inspection through script source. Verify a script with a proven widget operand links back to the exact component, and that unsupported instructions remain identifiable in the source and disassembly. Callback execution, simulated event state, script editing, compilation, and cache writes are outside scope.
