## ADDED Requirements

### Requirement: Archive-batched provider ranges release decoded archives
Generic and NPC provider range reads MUST release each decoded archive after its adjacent definition batch has been processed, including when archive loading, definition decoding, or the range post-decode hook throws.

#### Scenario: A range crosses archive boundaries
- **WHEN** a generic or NPC range reads definitions from more than one archive
- **THEN** each prior archive is disposed before the next archive is loaded and the final archive is disposed when the range completes

#### Scenario: A range operation fails
- **WHEN** loading a later archive, decoding a member, or running the range hook throws
- **THEN** any archive currently owned by the range operation is disposed and the original exception propagates
