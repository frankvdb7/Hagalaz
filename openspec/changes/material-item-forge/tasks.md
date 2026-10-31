## Implementation

- [x] A0: Centralize shared Material defaults and remove redundant page overrides, preserving specialized preview layouts.

- [x] A1: Replace custom controls and containers with existing Material components; remove superseded styles.
- [x] A2: Verify editing and zoom through Material harnesses and retain existing save/navigation regressions.
- [x] A3: Run admin build/tests, coverage, lint, Fallow, OpenSpec validation and desktop/phone browser QA; record limitations.


## Evidence

See `Hagalaz.Web.App/design-qa.md`, latest 2026-10-31 section. Admin build, 243 tests, focused coverage, Biome error gate, Fallow and strict OpenSpec validation pass. Browser verification uses isolated fixtures. Production authentication and live persistence remain unverified.
