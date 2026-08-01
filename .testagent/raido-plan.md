# Raido coverage plan

1. Repair the existing Coverlet test-project references so the configured collector is actually included.
2. Measure the baseline and rank uncovered Server code using Cobertura and CRAP analysis.
3. Add focused tests for isolated infrastructure, protocols, buffers, codecs, options, builders, proxies, and extensions.
4. Add behavior tests for reflection/executor paths, hub dispatch/lifecycle/filter/auth behavior, connection lifecycle, metrics, and pipe-reader edge cases.
5. Run both Raido test projects with coverage, generate the final report, and verify the 80% line threshold.

Edge-case follow-up:

1. Add explicit boundary tests for common buffers and server protocol infrastructure.
2. Correct the reader completion state so completed non-empty unparseable input fails fast.
3. Run focused edge tests, complete Raido suites, and regenerate combined coverage.

Usage-shaped follow-up:

1. Add common writer tests for opcode/size/payload and bit-to-byte packet composition.
2. Add server integration-shaped tests for registered codec/protocol resolution, builder usage, hub dispatch, and lifetime-manager sends.
3. Validate focused tests, full Raido suites, and combined coverage.
