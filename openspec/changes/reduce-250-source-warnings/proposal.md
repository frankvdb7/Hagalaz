# Proposal

## Why

Remaining C# warnings make it harder to spot new diagnostics in Rider. Reduce the current solution baseline by at least 250 distinct source warnings.

## What Changes

- Use Rider and JetBrains InspectCode to identify actionable warnings. Start with fixes that preserve observable behavior, such as redundant constructs and unused callback arguments.
- Find existing coverage and run it before each batch. Add and run focused tests against the unchanged production code where coverage is missing.
- Verify a net reduction of at least 250 warnings under the same inspection settings, with no new source warnings. Package upgrades, generated code, and inspection configuration changes are outside this pass.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

Affected C# code and its existing test projects. Public contracts and observable behavior are preserved.
