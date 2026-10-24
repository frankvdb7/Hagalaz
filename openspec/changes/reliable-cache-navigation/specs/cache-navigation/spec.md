## ADDED Requirements

### Requirement: Applied browsing context survives navigation
Cache Studio SHALL restore applied kind, query, offset and limit from the URL without issuing duplicate requests.

#### Scenario: Return from a definition
- **GIVEN** a browser showing an applied search on a later page
- **WHEN** the user opens a definition and returns with Back or the list link
- **THEN** the previous applied search and page are restored

#### Scenario: Draft search and shared links
- **GIVEN** a URL containing valid applied browsing parameters
- **WHEN** the user loads it and types an unsubmitted query
- **THEN** the URL and displayed request remain applied to the original query

### Requirement: Unsaved work is protected
Cache Studio SHALL use one confirmation policy for supported dirty editors and staged sprite files, including same-route ID navigation.

#### Scenario: Cancel definition change
- **GIVEN** unsaved edits on a definition
- **WHEN** another definition is selected and Keep editing is chosen
- **THEN** the current URL, selected definition and edited values remain unchanged

#### Scenario: Discard and navigate
- **GIVEN** unsaved edits
- **WHEN** Discard changes is chosen
- **THEN** the requested navigation completes without sending a mutation

#### Scenario: Mutation failure and success
- **GIVEN** an edited supported form
- **WHEN** its save fails or is cancelled
- **THEN** the edited values remain protected
- **WHEN** its save succeeds
- **THEN** those saved values become clean

#### Scenario: Pending write and browser unload
- **GIVEN** a pending cache mutation or unsaved local work
- **WHEN** navigation or browser unload is attempted
- **THEN** pending writes prevent in-app departure and native browser unload protection is requested where supported

#### Scenario: Cancel sign out
- **GIVEN** an authenticated administrator with unsaved edits
- **WHEN** sign out is attempted and Keep editing is chosen
- **THEN** authentication and the draft remain intact
- **WHEN** discard is accepted and navigation reaches the login route
- **THEN** the existing auth owner signs out exactly once
