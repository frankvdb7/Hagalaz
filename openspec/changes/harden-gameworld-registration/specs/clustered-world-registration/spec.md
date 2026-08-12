## ADDED Requirements

### Requirement: World configuration is explicit and valid
GameWorld SHALL require a positive logical world ID, a non-empty world name, a non-empty advertised host, and a TCP port from 1 through 65535 before it can become ready. The service MUST NOT substitute world ID `1` when `HAGALAZ_WORLD_ID` is absent.

#### Scenario: Missing world identity
- **WHEN** GameWorld starts without `HAGALAZ_WORLD_ID`
- **THEN** options validation fails and the world remains unready

#### Scenario: Invalid endpoint
- **WHEN** the advertised host is blank or the port is outside the TCP range
- **THEN** options validation fails and the world remains unready

### Requirement: Registration identifies and renews a process generation
Every online status SHALL include the logical world ID, unique process instance ID, generation/start timestamp, last-seen timestamp, client-reachable host and TCP port, and a lease expiry. The owning status service MUST retry initial publication and periodically renew the lease; readiness MUST remain false before the first successful publication and after renewal is lost.

#### Scenario: Initial publication failure
- **WHEN** the first status publication fails
- **THEN** the service retries and the world health check remains unhealthy

#### Scenario: Successful renewal
- **WHEN** a renewal is published before the lease expires
- **THEN** consumers retain the world as online and the local generation remains eligible for readiness

#### Scenario: Missed renewal
- **WHEN** no renewal is observed until the lease expires
- **THEN** the world is no longer online in the local world list without requiring a graceful offline event

### Requirement: Generations protect replacement and duplicate ownership
Consumers SHALL track live registrations by logical world and process instance. A stale offline event MUST NOT remove a newer replacement generation, and a second live generation for one logical world MUST make that world ambiguous and prevent the local process from becoming ready for that identity.

#### Scenario: Stale offline after replacement
- **WHEN** generation B is observed after generation A and an offline event for A arrives
- **THEN** generation B remains online and its configured endpoint remains advertised

#### Scenario: Duplicate live identity
- **WHEN** two different live instances advertise the same world ID
- **THEN** the conflict is observable and neither conflicting local generation is considered world-ready

### Requirement: World-list reconstruction is deterministic
A GameWorld that successfully registers SHALL request current world status from all live worlds. Status consumers SHALL republish complete current snapshots, expire leases locally, and update the world-list cache immediately when metadata, endpoint, or availability changes. Identical snapshots SHALL be idempotent and checksums SHALL change only after an actual cache-visible change.

#### Scenario: Restarted world reconstructs the list
- **WHEN** a world restarts while another world is already online
- **THEN** the restarted process receives and stores the existing world's status without waiting for that world to restart

#### Scenario: Endpoint changes
- **WHEN** an online status changes its advertised endpoint
- **THEN** the next world-list cache is rebuilt with the new endpoint and a new checksum

### Requirement: Readiness and shutdown preserve serving safety
`/health` SHALL remain unhealthy until valid initialization, endpoint startup, successful current-generation registration, and conflict-free ownership are established. `/alive` SHALL remain a liveness-only check. When shutdown begins, readiness and new world sign-ins SHALL be removed before the existing character snapshot flush; after that flush succeeds, the matching generation SHALL publish offline before the message bus stops.

#### Scenario: World is not registered
- **WHEN** the process is responsive but registration has not succeeded
- **THEN** `/alive` is healthy and `/health` is unhealthy

#### Scenario: Shutdown ordering
- **WHEN** application stopping begins
- **THEN** readiness and world sign-in admission are removed, the existing durable snapshot flush runs, and the matching offline status is published while MassTransit is still available

### Requirement: Contacts cleanup is generation-aware
Contacts SHALL retain the existing contact sign-out behavior for a graceful world-offline event, but SHALL remove a world session only when the offline event matches the currently observed instance and generation.

#### Scenario: Graceful contacts cleanup
- **WHEN** the current generation publishes offline
- **THEN** contacts are signed out and the matching world session is removed

#### Scenario: Delayed old cleanup event
- **WHEN** an offline event for an older generation arrives after a replacement is online
- **THEN** the replacement world session and its contacts remain registered

### Requirement: Deployment configuration enforces unique local world resources
Version-controlled local deployment configuration SHALL show at least two GameWorld resources with unique world IDs and collision-free TCP, HTTPS, and HTTP endpoints. Deployment documentation SHALL require one serving workload per identity, automatic restart, and world-aware readiness/liveness probes.

#### Scenario: Two local worlds
- **WHEN** the Aspire app is started with the checked-in multi-world configuration
- **THEN** both worlds have distinct identities, advertised endpoints, and health probes
