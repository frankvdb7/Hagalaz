## ADDED Requirements

### Requirement: Reference-aligned overview
The admin overview SHALL present the existing navigation, overview actions and data states in a responsive layout consistent with the supplied reference.

#### Scenario: View overview
- **WHEN** an administrator opens the overview
- **THEN** the hero, metrics, activity, quick actions and lower information panels are visible and navigation remains available

#### Scenario: Telemetry is unavailable
- **WHEN** telemetry is not connected
- **THEN** the overview identifies it as unavailable without displaying invented metrics

#### Scenario: Load archive counts
- **WHEN** an administrator requests archive counts
- **THEN** the existing store handles loading, results and errors
