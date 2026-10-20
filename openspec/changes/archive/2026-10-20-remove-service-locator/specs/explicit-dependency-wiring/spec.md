## ADDED Requirements

### Requirement: Global service locator is absent

The GameWorld application SHALL not initialize or expose a static global service locator, compatibility wrapper, or arbitrary service-resolution facade.

#### Scenario: GameWorld starts without global provider registration

- **WHEN** the GameWorld application builds its service provider and starts
- **THEN** startup does not call `SetLocatorProvider` or register a replacement global lookup mechanism

#### Scenario: Repository contains no obsolete locator references

- **WHEN** production and test source is searched after the migration
- **THEN** no `ServiceLocator`, `ServiceLocator.Current`, or `SetLocatorProvider` implementation or stale comment remains

### Requirement: Former locator consumers receive typed dependencies

Area scripts and `SpellBookTab` SHALL receive the specific services they require through their existing DI construction paths.

#### Scenario: Area script initializes configured respawn coordinates

- **WHEN** an area script is constructed with `IOptions<WorldOptions>` and initialized with an area
- **THEN** its default respawn location uses the configured world spawn coordinates without service lookup during initialization

#### Scenario: Spellbook ancient-spell helpers use explicit construction dependencies

- **WHEN** `SpellBookTab` is constructed through DI and its ancient spell helpers are used
- **THEN** the helpers construct their fixed four-spell arrays using the injected `IProjectileBuilder`

### Requirement: SpellBookTab initialization remains non-recursive and bounded

The spellbook SHALL preserve its existing lifecycle boundaries and SHALL NOT add recursive or repeated initialization.

#### Scenario: Static spellbook initialization

- **WHEN** `SpellBookTab` is first loaded
- **THEN** its static constructor loads teleports only and does not open, refresh, or recursively construct a spellbook

#### Scenario: Spellbook opening

- **WHEN** `OnOpen` is called for a spellbook instance
- **THEN** it registers the existing handlers once for that open operation and does not invoke `OnOpen` or a new spell-loading loop from a refresh or constructor path
