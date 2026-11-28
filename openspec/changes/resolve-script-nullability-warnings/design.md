# Design

## Context

Duel setup, trading, and character design run in GameWorld. Their scripts hold references acquired during widget opening or participant setup and released on close or cleanup. The initial rebuild identified 44 compiler diagnostics in DuelArenaScript, 26 in DuelScreenScript, 22 in TradingCharacterScript, and 13 in DesignInterface.

## Goals / Non-Goals

- Remove at least 100 unique source diagnostics on the same build configuration, without introducing new diagnostics.
- Preserve widget component mappings, trade and duel lifecycle guarantees, and character appearance settings.
- Keep dependency upgrades and changes to gameplay rules outside this change.

## Decisions

- Add and run characterization tests for each batch before editing production code. Use public widget callbacks and the existing trade and duel fixtures to exercise the lifecycle.
- Check participant and widget state at the boundary that consumes it. Use the existing session state and cleanup paths, and keep optional callbacks explicitly nullable.
- Share the duel rule click handler while retaining each component-to-rule mapping and the existing update order.
- Initialize character design state where the widget acquires it and verify that opening and submitting the widget retain the existing appearance behavior.

## Risks / Trade-offs

- Widget callbacks can outlive the state they originally observed. Test close and reopen paths and retain existing input ownership checks.
- Trade and duel changes must preserve acceptance, settlement, and publication ordering. Run the existing regression suites for these paths before and after the edit.
- MSBuild may print diagnostics again in its summary. Count unique source locations and diagnostic IDs using the same rebuild settings.
