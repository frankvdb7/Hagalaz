## MODIFIED Requirements

### Requirement: Effect identity controls material previews
The viewer SHALL select advanced previews from source effect IDs and parameters, covering IDs 1, 2, 4, 5, 6, 7, 8 and 9. It SHALL label approximations and capability fallbacks and preserve original identity.

#### Scenario: Supported effect
- **GIVEN** an available supported effect material
- **WHEN** a model using it is selected
- **THEN** the corresponding preview uses its decoded parameters and displays its fidelity classification

#### Scenario: Unresolved effect
- **WHEN** a material uses an unknown effect
- **THEN** a base-material fallback is displayed with the source ID and an unsupported message

#### Scenario: Absent scene inputs
- **WHEN** an effect requires unavailable terrain, shoreline or environment inputs
- **THEN** the preview identifies its fallback without claiming scene-effect parity

#### Scenario: Effect 3 with available inputs
- **WHEN** a model uses effect 3 and the client behavior can be verified with inputs available in the preview
- **THEN** the preview applies that behavior using the source effect parameters

#### Scenario: Effect 3 requires absent scene inputs
- **WHEN** a model uses effect 3 and its required scene inputs are unavailable
- **THEN** the preview shows the base material and identifies the unavailable scene effect by name and source ID
