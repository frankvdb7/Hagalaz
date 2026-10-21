# Tasks

## 1. Consolidate snapshot bootstrap

- [x] 1.1 Move connection lookup, feature discovery, and observation-window startup into the existing `ContactSnapshotLoader`; update both consumers to start it beside their existing sign-in work, and verify the focused consumer tests preserve request identity and sign-in publication.
- [x] 1.2 Add lobby consumer tests for timeout, request fault, and cancellation, asserting sign-in continuation for timeout/fault and snapshot-gate/window cleanup on every outcome.

## 2. Verify the scoped refactor

- [x] 2.1 Run the focused GameWorld consumer tests and rerun `npx jscpd --reporters ai --summary Hagalaz.Services.GameWorld`; verify the duplicated snapshot-bootstrap clone is gone and no observable sign-in behavior changed.
