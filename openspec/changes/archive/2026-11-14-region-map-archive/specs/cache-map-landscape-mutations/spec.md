# Spec Delta

## ADDED Requirements

### Requirement: Landscape saves never overwrite a landscape they did not check

The Cache service MUST write a landscape save only over the stored landscape it checked was readable. When another write changes the region's landscape after that check, the save MUST check the new stored landscape again before writing. The service does not order two whole-landscape saves of the same region: whichever is written last is stored.

#### Scenario: Another save lands between the check and the write

- **GIVEN** a save has checked that region 12850's stored landscape is readable
- **WHEN** another save writes region 12850's landscape before the first save writes
- **THEN** the first save checks the newly stored landscape before writing, and a later read returns the first save's placements

#### Scenario: The landscape becomes unreadable before the write

- **GIVEN** a save has checked that a region's stored landscape is readable
- **WHEN** the stored landscape is replaced by data that cannot be decoded before the save writes
- **THEN** the service responds that the landscape cannot be replaced and writes nothing

### Requirement: Landscape saves that change nothing are not written

The Cache service MUST NOT write a region's landscape when the submitted placements encode to the same bytes as the stored placements re-encoded, and MUST still report the save as successful.

#### Scenario: Saving the stored placements again

- **GIVEN** a region with a readable landscape
- **WHEN** an administrator submits exactly the placements that are stored
- **THEN** the service reports the landscape as updated and the landscape file, its version and the reference table are unchanged

#### Scenario: Retrying a save that already succeeded

- **GIVEN** a landscape save that was written
- **WHEN** the same save is submitted again
- **THEN** nothing is written, so the region's world map entry is not regenerated because of the retry
