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
A GameWorld that successfully registers SHALL request current world status from all live worlds. Status consumers SHALL republish complete current snapshots, expire leases locally, and update the world-list cache immediately when metadata, endpoint, or availability changes. Identical snapshots SHALL be idempotent and checksums SHALL change only after an actual cache-visible change. The shared cache key and checksum SHALL be derived deterministically from the metadata snapshot and MUST NOT depend on process-local counters.

#### Scenario: Restarted world reconstructs the list
- **WHEN** a world restarts while another world is already online
- **THEN** the restarted process receives and stores the existing world's status without waiting for that world to restart

#### Scenario: Endpoint changes
- **WHEN** an online status changes its advertised endpoint
- **THEN** the next world-list cache is rebuilt with the new endpoint and a new checksum

#### Scenario: Cache survives a process restart
- **WHEN** a restarted process reads shared Redis after observing the same metadata snapshot
- **THEN** it uses the same cache key and checksum, while a different metadata snapshot uses a different cache key and cannot return the old entry

### Requirement: Readiness and shutdown preserve serving safety
`/health` SHALL remain unhealthy until valid initialization, endpoint startup, successful current-generation registration, and conflict-free ownership are established. `/alive` SHALL remain a liveness-only check. When shutdown begins, readiness and new world sign-ins SHALL be removed before the existing character snapshot flush; after that flush succeeds, the matching generation SHALL publish offline before the message bus stops.

#### Scenario: World is not registered
- **WHEN** the process is responsive but registration has not succeeded
- **THEN** `/alive` is healthy and `/health` is unhealthy

#### Scenario: Shutdown ordering
- **WHEN** application stopping begins
- **THEN** readiness and world sign-in admission are removed, the existing durable snapshot flush runs, and the matching offline status is published while MassTransit is still available

### Requirement: Contacts cleanup is generation-aware
Contacts SHALL retain the existing contact sign-out behavior for a graceful world-offline event, expire sessions whose leases are not renewed, and SHALL remove a world session only when the offline event matches the currently observed instance and generation. `IContactSessionService` SHALL own contact-session removal and sign-out message construction. Consumers SHALL resolve it with their normal scoped lifetime, and hosted lease expiry SHALL create a scope before invoking it. Bulk cleanup SHALL remove a session only if its key still contains the snapshotted expected value, and SHALL publish sign-out only after that conditional removal succeeds.

#### Scenario: Graceful contacts cleanup
- **WHEN** the current generation publishes offline
- **THEN** contacts are signed out and the matching world session is removed

#### Scenario: Delayed old cleanup event
- **WHEN** an offline event for an older generation arrives after a replacement is online
- **THEN** the replacement world session and its contacts remain registered

#### Scenario: Crash lease expiry
- **WHEN** a world stops renewing without publishing an offline event
- **THEN** Contacts expires that generation and signs out its contacts

#### Scenario: Crash cleanup removes stale contact sessions
- **WHEN** a crashed world's contacts are signed out after lease expiry
- **THEN** their `ContactSessionStore` entries are removed so the same players can reconnect

#### Scenario: Replacement session survives stale cleanup
- **WHEN** a snapshotted contact disconnects and reconnects on another world before bulk cleanup reaches that entry
- **THEN** the replacement session remains registered and no sign-out is published for the replacement

#### Scenario: Surviving generation after offline
- **WHEN** one generation goes offline while another live generation for the same world remains
- **THEN** Contacts retains the surviving generation and does not sign out its contacts

### Requirement: Status monitoring survives transient broker failures
The Contacts status monitor SHALL handle initial status-request and individual expired-world cleanup failures at the operation boundary. Known broker, timeout, invalid-operation, and non-shutdown cancellation failures SHALL be logged without terminating periodic monitoring; cancellation requested for service shutdown SHALL still stop the monitor.

#### Scenario: Cleanup failure does not stop later expiry processing
- **WHEN** cleanup publication fails for one expired world and a later expired world is processed
- **THEN** the first failure is logged and cleanup for the later world is still attempted

### Requirement: Broadcast status subscriptions are process-local
Every GameWorld process SHALL receive each status request and online/offline status publication independently. The status consumers MUST NOT share a durable auto-configured queue between processes; each process SHALL use a unique temporary subscription endpoint.

#### Scenario: Two GameWorld processes observe the same publication
- **WHEN** two processes with different instance IDs are connected to the same RabbitMQ host
- **THEN** both process-local registration stores receive the status publication

### Requirement: Legacy client endpoints remain connectable
The 742 world-list and lobby response wire contracts SHALL continue to advertise the client-compatible host field without claiming to serialize a per-world port. Aspire's local multi-world configuration SHALL bind both worlds to the existing client TCP port on distinct loopback hosts.

#### Scenario: Two local worlds use the host-only client contract
- **WHEN** the client selects either local world
- **THEN** it connects to the configured client port using that world's advertised loopback host

### Requirement: Deployment configuration enforces unique local world resources
Version-controlled local deployment configuration SHALL show at least two GameWorld resources with unique world IDs and collision-free TCP, HTTPS, and HTTP endpoints. Local TCP endpoints SHALL be explicit proxyless target-port 443 endpoints when both worlds use the legacy fixed client port. GameWorld SHALL apply `World:ListenHost` only to the legacy TCP listener; HTTP and HTTPS management listeners SHALL bind loopback for Aspire proxy compatibility. Deployment documentation SHALL require one serving workload per identity, automatic restart, and world-aware readiness/liveness probes.

#### Scenario: Two local worlds
- **WHEN** the Aspire app is started with the checked-in multi-world configuration
- **THEN** both worlds have distinct identities, advertised endpoints, health probes, and no shared DCP TCP proxy host port
