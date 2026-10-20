## ADDED Requirements

### Requirement: Frontend health findings SHALL be reduced without suppressing valid analysis

The package-local frontend implementation SHALL keep Fallow's existing health thresholds and SHALL report no health findings for the current launcher command validation, cache request runners, or types-page template scope.

#### Scenario: Full health report is clean for the scoped findings

- **WHEN** Fallow analyzes the complete `Hagalaz.Web.App` source graph
- **THEN** the four current health findings are absent and no broad health rule or threshold override is required

#### Scenario: Changed-file audit remains clean

- **WHEN** the package-local Fallow audit analyzes the change against its Git base
- **THEN** it reports no introduced dead-code, complexity, duplication, or styling issue

### Requirement: Cache request lifecycle behavior SHALL remain stable after decomposition

The cache admin read, mutation, and sprite surfaces SHALL preserve their existing request lifecycle: clear the prior error, mark loading before the request, publish the successful result, map failures using the existing detail/title/message/fallback precedence, and clear loading after success or failure.

#### Scenario: A successful cache request publishes its result

- **WHEN** a cache page request resolves successfully
- **THEN** the corresponding result is updated and loading is reset with no error message

#### Scenario: A failed cache request maps and clears state

- **WHEN** a cache page request rejects with a detail, title, message, or unknown error
- **THEN** the existing precedence selects the error text, the error signal is updated, and loading is reset

### Requirement: Launcher IPC validation SHALL preserve command dispatch semantics

The launcher API handler SHALL reject missing, malformed, and unknown command arguments with the existing warnings, SHALL dispatch known commands through the existing handler map, and SHALL pass trailing arguments to the selected handler in their existing order.

#### Scenario: Invalid launcher commands are rejected

- **WHEN** an IPC callback receives no command, a non-object command, an object without a string `commandType`, or an unknown command type
- **THEN** no command handler runs and the corresponding existing warning is logged

#### Scenario: A valid launcher command preserves trailing arguments

- **WHEN** an IPC callback receives a registered command followed by arguments
- **THEN** the registered handler runs with the event and the same trailing arguments in the same order

### Requirement: Types page decomposition SHALL preserve the existing admin surface

The types manager route SHALL render the existing read cards and mutation forge through focused components without changing route ownership, form fields, service calls, endpoint payloads, or visible labels.

#### Scenario: Existing types actions remain available

- **WHEN** an administrator opens the types manager route
- **THEN** archive, by-id, range, search, NPC, object, varp, and config actions remain available with their existing controls and results
