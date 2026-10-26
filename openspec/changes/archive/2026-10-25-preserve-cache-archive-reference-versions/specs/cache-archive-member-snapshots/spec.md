## ADDED Requirements

### Requirement: Rewritten archive versions advance from reference-table metadata

High-level writes to an existing archive MUST advance that archive's reference-table entry version by one from its pre-write value. Version advancement MUST remain correct when the physical container has no version trailer.

#### Scenario: Consecutive archive-member replacements

- **WHEN** two high-level writes replace members of an archive whose reference-table entry starts at version 5
- **THEN** the entry version is 7, the final member bytes are readable, and the physical container keeps the existing trailer-free encoding
