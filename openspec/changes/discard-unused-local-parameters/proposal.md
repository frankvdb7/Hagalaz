# Proposal

## Why

The initial Rider inspection baseline contains 4,616 warning findings. Removing confirmed redundant constructs and clarifying unused callback parameters reduces inspection noise without changing runtime behavior.

## What Changes

- Resolve at least 500 Rider warnings from the recorded baseline through semantics-preserving simplifications, including redundant imports, type arguments, array type specifications, casts, namespace qualifiers, and clarified unused callback parameters.
- Preserve delegate signatures, callback bodies, registrations, and behavior.
- Rebuild, test affected projects, and rerun the same Rider inspection.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Changes are limited to redundant C# syntax and intentionally unused local-function and lambda parameters. There are no API or runtime behavior changes.
