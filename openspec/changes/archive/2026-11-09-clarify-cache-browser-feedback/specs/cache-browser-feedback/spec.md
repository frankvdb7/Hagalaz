## ADDED Requirements

### Requirement: Request feedback is exclusive and recoverable
The browser SHALL display one list-card content state for its applied request and SHALL retry that request without changing applied context.

#### Scenario: Delayed request replaces visible results
- **GIVEN** a populated list
- **WHEN** a new search or page request is pending
- **THEN** loading feedback replaces old rows until the new request settles

#### Scenario: Retry preserves applied context
- **GIVEN** a failed request and unsubmitted draft text
- **WHEN** Retry is selected
- **THEN** the original applied kind, query, offset and limit are requested once and the draft stays intact

#### Scenario: Empty response
- **GIVEN** a successful request returning no definitions
- **WHEN** its response is displayed
- **THEN** an empty state is shown without loading or error feedback

### Requirement: Entry points reflect supported capabilities
The browser SHALL describe actual search capabilities and the portal SHALL expose the modern sprite browser without removing legacy tools.

#### Scenario: Sprite discovery
- **WHEN** the Overview sprite action or global Sprites search result is selected
- **THEN** the portal opens /cache/types?kind=sprites

#### Scenario: ID-only lookup and legacy search
- **WHEN** a type without name search is selected
- **THEN** its controls describe exact ID lookup and available range browsing
- **WHEN** legacy sprite search is used
- **THEN** its wording describes ID-fragment search instead of name search
