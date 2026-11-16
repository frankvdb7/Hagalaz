# CI performance feedback

## Purpose

Provide trustworthy benchmark feedback while avoiding obsolete pull request CI work.

## ADDED Requirements

### Requirement: Comparable benchmark feedback
The CI system SHALL compare PR benchmark results against a valid main baseline and visibly report an unavailable or failed comparison.

#### Scenario: Comparison succeeds
- **WHEN** a PR benchmark report and compatible main baseline exist
- **THEN** the comparison reports results and regression evidence with units.

#### Scenario: Comparison cannot run
- **WHEN** the report, baseline, or comparison is unavailable or fails
- **THEN** the check reports failure or explicitly unavailable feedback rather than a successful comparison.

#### Scenario: Untrusted PR
- **WHEN** a fork PR runs benchmarks
- **THEN** it cannot publish or modify the baseline.

### Requirement: Complete benchmark coverage
Main and explicit runs SHALL produce complete benchmark reports. A reduced PR run SHALL cover all benchmarks relevant to the changed source or fall back to the complete run if impact is uncertain.

#### Scenario: Impact is uncertain
- **WHEN** changed paths cannot be mapped confidently to benchmark coverage
- **THEN** the PR runs the complete benchmark suite.

### Requirement: Superseded PR work
The CI system SHALL cancel superseded PR runs independently for each workflow without cancelling main or scheduled runs.

#### Scenario: Consecutive PR pushes
- **WHEN** a newer commit starts the same PR workflow
- **THEN** the older run is cancelled and other workflows and main runs remain independent.
