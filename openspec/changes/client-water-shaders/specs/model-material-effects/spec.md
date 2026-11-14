## MODIFIED Requirements

### Requirement: Effect identity controls material previews
The viewer SHALL select advanced previews from source effect IDs and parameters, covering IDs 1, 2, 4, 5, 6, 7, 8 and 9. It SHALL label approximations and capability fallbacks and preserve original identity.

#### Scenario: Supported effect
- **GIVEN** an available supported effect material
- **WHEN** a model using it is selected
- **THEN** the corresponding preview uses its decoded parameters and displays its fidelity classification

#### Scenario: Unresolved effect
- **WHEN** a material uses category 3 or an unknown effect
- **THEN** a base-material fallback is displayed with the source ID and an unsupported message

#### Scenario: Absent scene inputs
- **WHEN** an effect requires unavailable terrain, shoreline or environment inputs
- **THEN** the preview identifies its fallback without claiming scene-effect parity

#### Scenario: Source water program

- **WHEN** the map viewer can run the client program for effect 2, 4, 5, 8 or 9
- **THEN** it renders the client water stages and identifies the ported behavior

#### Scenario: HD water without an environment

- **WHEN** effect 8 or 9 is drawn in a standalone preview or in a scene without a usable environment
- **THEN** the preview uses the client's base-material fallback and identifies the missing scene input
