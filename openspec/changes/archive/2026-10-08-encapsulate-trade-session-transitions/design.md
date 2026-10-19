# Design

## Context

`TradeSessionState` is a private nested type in `TradingCharacterScript`. Its owner already serializes lifecycle work with `session.Gate`; only the owner orchestrates transitions.

## Goals / Non-Goals

**Goals:** Make the existing lifecycle transitions explicit and reject invalid transition calls.

**Non-Goals:** Add a state-machine abstraction, expose trade state publicly, or change the gate, transaction, cleanup, exchange, or refund behavior.

## Decisions

- Keep the current private nested session type and make `State` privately settable.
- Add four small methods for the observed transitions: `BeginCompletion`, `MarkCompleted`, `MarkCancelled`, and `ReturnToActive`.
- Each method checks only its current expected source state and throws `InvalidOperationException` on misuse; do not add a generic setter or transition helper.
- Keep terminal state changes immediately before `Commit`, so post-commit publication exceptions still leave the session terminal.
- Verify the lifecycle through existing public trade-flow tests, including post-commit completion and refund publication failures. Do not expose private state just for direct unit testing.

## Risks / Trade-offs

- A missed direct assignment would fail to compile after making the setter private; a repository search for assignments verifies the migration.
- Transition guards may surface latent orchestration mistakes as exceptions. The current call sites are serialized and follow the specified source states.
