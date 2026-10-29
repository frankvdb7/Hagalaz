## ADDED Requirements

### Requirement: Responsive cache browsing presentation
The cache workspace SHALL present its existing categories, search and results in a responsive layout based on the supplied reference.

#### Scenario: Browse definitions
- **WHEN** an administrator opens a supported category
- **THEN** category navigation, search, real results and pagination remain available

#### Scenario: Data is unavailable
- **WHEN** a request fails or a result is empty
- **THEN** the workspace displays the existing error or empty state without inventing data

#### Scenario: Inspect a result from its actions menu
- **WHEN** an administrator selects inspect from a row's actions menu
- **THEN** the matching definition detail opens with the current browse context preserved

#### Scenario: Keep the current page visible while paging
- **WHEN** an administrator requests another page for the same category and search query
- **THEN** the last successful results remain visible until the new page arrives, the paginator range stays aligned with those rows and keeps its normal appearance, and a different search query does not show stale matches
