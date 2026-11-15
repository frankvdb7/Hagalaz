# Tasks

## 1. Cache service

- [x] 1.1 Implement revision-742 preview execution for literals, typed locals and stacks, branches and switches, direct calls, simulated variables, and verified visual setters needed by the preview fixtures. Run focused tests for state changes, unsupported instructions, cancellation, and instruction and call-depth limits.
- [x] 1.2 Add the authenticated event endpoint. Verify client open order, cached mouse-pressed dispatch, manual trigger matching, event parameter binding, snapshot continuity, and rollback on failure with endpoint fixture tests.

## 2. Widget detail

- [x] 2.1 Add interactive mode and manual actions in Logic. Run Angular tests for starting the preview, routing clicks and manual events, rendering returned effects, and showing filtered and failed events.
- [x] 2.2 Reset to cached layout and ignore responses for a reset or departed preview. Run Angular regressions for reset and route changes with an event pending.

## 3. Integration

- [ ] 3.1 Verify the signed-in preview against the browser runtime acceptance check in `browser-widget-script-preview`.
