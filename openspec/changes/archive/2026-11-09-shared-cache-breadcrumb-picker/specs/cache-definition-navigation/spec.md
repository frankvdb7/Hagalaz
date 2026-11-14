## ADDED Requirements

### Requirement: Shared cache breadcrumb picker
Every cache definition detail route SHALL render a picker in the main breadcrumb using the current page's definition store and type. It SHALL navigate to that type's existing detail route, accept ID zero and preserve other detail functionality.

#### Scenario: Jump within a type
- **WHEN** an administrator selects an ID from a cache detail picker
- **THEN** the existing route for that same type loads the selected definition

### Requirement: Supported search capabilities
Items and NPCs SHALL offer debounced name autocomplete through their existing stores. Other types SHALL offer exact nonnegative ID navigation without issuing unsupported name-search requests. Picker labels SHALL identify the current type and available input.

#### Scenario: ID-only definition
- **WHEN** an administrator enters text into an animation picker
- **THEN** it requests a valid ID and makes no name-search request

### Requirement: Picker lifecycle
The picker SHALL preserve keyboard selection, Escape and outside-click dismissal, focus restoration and removal of its breadcrumb template when its page is destroyed.

#### Scenario: Leave a detail page
- **WHEN** the administrator navigates to a different cache detail type or leaves cache details
- **THEN** the toolbar displays only the active page's breadcrumb and picker
