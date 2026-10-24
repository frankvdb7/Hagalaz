## ADDED Requirements

### Requirement: Dedicated item inspection
The admin SHALL provide Item Forge at the existing item detail route with a breadcrumb autocomplete picker, Overview, Interactions, Equipment, Variants and Raw data tabs, and an identity inspector. It SHALL remain usable at phone widths and use the main toolbar breadcrumb.

#### Scenario: Inspect an item
- **WHEN** an administrator opens an item detail route
- **THEN** the workspace displays that item's returned data in Material components without offering unsupported saves

### Requirement: Accurate cache semantics
The workspace SHALL preserve ordered action slots and distinguish absent values, zero and false. It SHALL label cache base value separately from market price and runtime state. Relationship links SHALL use explicit nonnegative IDs only. The API SHALL expose decoded note, lending, bought template references and inventory model ID as read-only fields.

#### Scenario: Inspect sparse actions and zero references
- **WHEN** a response contains empty action slots and a relationship ID of zero
- **THEN** empty slots retain their positions and the zero reference links to item zero

### Requirement: Existing resource ownership
Item Forge SHALL reuse CacheItemsStore and CacheItemsService for selection and search. The picker SHALL search names through the existing search resource and accept valid nonnegative IDs. Loading, errors and retry SHALL remain visible and route changes SHALL replace the selected item.

#### Scenario: Navigate between items
- **WHEN** an administrator selects an autocomplete suggestion or enters a valid item ID
- **THEN** the existing item route loads the selected ID and displays only its matching result

#### Scenario: Read fails
- **WHEN** the item request fails
- **THEN** the page displays the error and offers a retry without inventing item data


#### Scenario: Close the picker
- **WHEN** an administrator presses Escape or clicks outside the picker
- **THEN** it closes and returns focus to the item breadcrumb button
