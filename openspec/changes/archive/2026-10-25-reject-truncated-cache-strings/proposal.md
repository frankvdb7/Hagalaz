# Proposal

## Why

The shared null-terminated string reader converts EOF into byte `0xFF`, so a corrupt definition string without its terminator keeps appending characters and never returns or reports a decoding failure.

## What Changes

- Make `ReadString` throw `EndOfStreamException` when the stream ends before the null terminator.
- Add a regression through the real object-definition codec.

## Non-goals

- Do not add timeouts, cancellation, retries, or a new parsing abstraction.
- Do not change valid string decoding or the format's terminator.

## Acceptance Criteria

- A terminated string continues to decode normally.
- An object definition with an unterminated name fails promptly with `EndOfStreamException`.

## Stop Conditions

- If the cache format permits EOF as a string terminator for any supported definition type, stop and document that format distinction before broadening the behavior.
