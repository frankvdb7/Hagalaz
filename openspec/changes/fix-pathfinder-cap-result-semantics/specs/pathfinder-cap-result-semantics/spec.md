## ADDED Requirements

### Requirement: Dumb path cap exhaustion is unsuccessful
`DumbPathFinder` SHALL report an unsuccessful `IPath` when its safety step limit is reached before the requested X/Y destination, and SHALL preserve the traversed points and progress state accumulated before the limit.

#### Scenario: A short clear simple path reaches its destination
- **WHEN** a clear simple path requires fewer than the safety limit of steps
- **THEN** the result SHALL be successful and its final point SHALL equal the requested destination

#### Scenario: A simple path exceeds the safety limit
- **WHEN** a clear simple path requires more steps than the safety limit
- **THEN** the result SHALL be unsuccessful, SHALL contain only the points traversed before the rejected step, SHALL report the safety-limit step count, and SHALL report `MovedNear` as true when traversal moved from the source

#### Scenario: A simple path reaches the destination on the final permitted step
- **WHEN** a clear simple path requires exactly the safety limit of steps
- **THEN** the result SHALL be successful and SHALL contain the requested destination as its final point

### Requirement: Projectile path cap exhaustion is unsuccessful
`ProjectilePathFinder` SHALL enforce the existing pathfinder safety step limit while tracing either dominant axis and SHALL report an unsuccessful `IPath` when the limit is reached before the requested target tile.

#### Scenario: A short clear projectile trace reaches its target
- **WHEN** a same-plane projectile trace requires fewer than the safety limit of steps and encounters no LOS blocker
- **THEN** the result SHALL be successful and SHALL contain only the requested target tile

#### Scenario: A projectile trace exceeds the safety limit
- **WHEN** a same-plane projectile trace requires more steps than the safety limit
- **THEN** the result SHALL be unsuccessful, SHALL not append the target tile, SHALL report the safety-limit step count, and SHALL report `MovedNear` as true after more than one trace step

#### Scenario: A projectile trace reaches the target on the final permitted step
- **WHEN** a same-plane projectile trace requires exactly the safety limit of steps and encounters no LOS blocker
- **THEN** the result SHALL be successful and SHALL contain the requested target tile

### Requirement: Existing path result semantics remain coherent
Cap handling SHALL use the existing `Path` fields and SHALL not change pre-cap collision behavior or the derived destination properties.

#### Scenario: Collision failure occurs before the cap
- **WHEN** either finder encounters its existing blocking collision condition before the safety limit
- **THEN** the result SHALL remain unsuccessful with the existing points, step count, and progress indication for that collision failure

#### Scenario: A successful one-step path reaches its destination
- **WHEN** either finder completes a one-step path without collision
- **THEN** `Successful` SHALL be true, `Steps` SHALL be zero, `ReachedDestination` SHALL be true, and `MovedNearDestination` SHALL be false

#### Scenario: A capped path has not reached its destination
- **WHEN** either finder exhausts its safety limit before reaching the requested destination
- **THEN** `ReachedDestination` and `MovedNearDestination` SHALL both be false because `Successful` is false
