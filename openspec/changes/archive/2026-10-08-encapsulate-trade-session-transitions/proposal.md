# Proposal

## Why

`TradingCharacterScript` currently writes enum values directly into `TradeSessionState.State`, so lifecycle intent and legal transition rules are spread across orchestration branches. Named transitions make the existing lifecycle explicit while keeping state ownership in the session object.

## What Changes

- Make `TradeSessionState.State` privately settable.
- Add only the current domain transitions: begin completion, mark completion, mark cancellation, and return to active after failed completion.
- Replace direct production assignments while preserving the session gate and terminal-before-commit ordering.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `trading-completion`: specify session-owned, intent-named state transitions and their existing legal lifecycle paths.

## Impact

The change is limited to `TradingCharacterScript`, its existing focused trade tests, and the `trading-completion` OpenSpec capability. It adds no public API, dependency, generic state-machine mechanism, or service.

## Scope Boundary

### In Scope

- Encapsulating the private trade session state and replacing its production writes.
- Retaining the existing terminal publication-failure and cleanup regressions.

### Non-Goals

- Changing trade acceptance, exchange, cancellation, recovery, or transaction behavior.
- Changing item-container transaction architecture or adding state-machine infrastructure.

### Acceptance Criteria

- Every production state change uses one of the four named transitions.
- Only the current lifecycle transitions are allowed; invalid calls throw `InvalidOperationException`.
- Completed/cancelled state is set before the transaction commit that may publish and throw.
- Existing trade lifecycle regressions and strict OpenSpec validation pass.

### Stop Conditions

- Stop if satisfying these criteria requires changing session ownership, the session gate, or trade transaction ordering.
