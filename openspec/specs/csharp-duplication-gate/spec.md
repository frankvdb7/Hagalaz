# csharp-duplication-gate Specification

## Purpose
Make C# copy/paste duplication visible while preventing newly introduced clones from passing pull-request verification without making historical duplication a blanket failure.

## Requirements

### Requirement: Repository C# duplication is reported

The repository SHALL scan maintained C# production and test source for duplicate code and SHALL identify both locations of each reported clone.

#### Scenario: Maintained C# contains an existing clone
- **WHEN** the duplication scan analyzes the repository
- **THEN** the report identifies the clone and both source locations

#### Scenario: Generated or build output exists
- **WHEN** generated, migration, or build-output C# files are present
- **THEN** those files do not contribute duplication findings

### Requirement: Pull requests are gated on new duplication

The pull-request check SHALL compare its C# findings with the target branch and SHALL fail when a new clone above the configured minimum size is found.

#### Scenario: Only historical clones are present
- **WHEN** a pull request adds no new C# clone relative to its target branch
- **THEN** the duplication gate passes while reporting historical clones

#### Scenario: A new qualifying clone is introduced
- **WHEN** a pull request introduces a C# clone above the configured minimum size
- **THEN** the duplication gate fails and identifies the new clone

#### Scenario: A new clone is removed
- **WHEN** the qualifying clone is removed from the pull request
- **THEN** the duplication gate passes again

### Requirement: Misconfigured scans fail visibly

The duplication check SHALL fail if it analyzes no relevant source and SHALL use an available target-branch reference for pull-request comparison.

#### Scenario: No C# source is analyzed
- **WHEN** the scan configuration excludes or fails to match all relevant source
- **THEN** the check fails rather than reporting a successful empty scan

#### Scenario: Target branch reference is unavailable
- **WHEN** the configured comparison reference is missing
- **THEN** the check fails rather than accepting an empty or unrelated baseline

#### Scenario: CI runs on a push
- **WHEN** the CI workflow runs for a push rather than a pull request
- **THEN** it does not construct a comparison reference from an absent pull-request target branch

### Requirement: CI publishes inspectable findings

The pull-request check SHALL make duplicate locations available in its logs and a Code Scanning compatible report.

#### Scenario: Duplication scan completes
- **WHEN** the pull-request check scans C# source
- **THEN** it emits a report containing the detected clone locations for Code Scanning upload
