## 1. Contracts and configuration

- [x] 1.1 Add typed advertised endpoint and registration timing options, options validation, explicit development/Aspire configuration, and remove the world-ID fallback (Req: World configuration is explicit and valid; Test: invalid options).
- [x] 1.2 Extend online/offline messages and all status mappings/consumers with instance, generation, lease, and endpoint data (Req: Registration identifies and renews a process generation).
- [x] 1.3 Keep the legacy host-only client wire contract honest by removing the unencoded world-list port field and documenting/configuring the existing client TCP port (Req: Legacy client endpoint compatibility; Test: two-world same-port connection).

## 2. Registration lifecycle and readiness

- [x] 2.1 Add the single observed-registration store with generation comparison, conflict detection, offline fencing, and lease expiry (Req: Generations protect replacement and duplicate ownership; Tests: stale offline, duplicate, expiry).
- [x] 2.2 Replace one-shot status publication with retrying initial registration, periodic renewal, reconstruction request, and bounded shutdown offline publication (Req: Registration identifies and renews a process generation; Req: Readiness and shutdown preserve serving safety).
- [x] 2.3 Add startup-task lifecycle reporting and world-aware health/readiness plus world sign-in admission checks (Req: Readiness and shutdown preserve serving safety; Tests: unregistered readiness and shutdown state).
- [x] 2.4 Exclude status consumers from shared endpoint auto-registration and bind a unique temporary per-process broadcast endpoint (Req: Per-process broadcast subscriptions; Test: status endpoint registration).

## 3. World list and contacts behavior

- [x] 3.1 Apply registration transitions to `WorldInfoService`, deterministically invalidate HybridCache, expire stale entries, and preserve the existing lobby flow (Req: World-list reconstruction is deterministic; Tests: endpoint/checksum/reconstruction).
- [x] 3.2 Make Contacts world sessions generation-aware while preserving current contact cleanup (Req: Contacts cleanup is generation-aware; Tests: graceful and delayed-old cleanup).
- [x] 3.3 Expire Contacts leases periodically and clean up only when no surviving generation remains (Req: Contacts cleanup is generation-aware; Tests: crash expiry and survivor offline).
- [x] 3.4 Make world-list cache keys and checksums deterministic from metadata rather than process-local counters, and evict captured contact sessions after successful crash/graceful cleanup (Req: World-list reconstruction is deterministic; Req: Contacts cleanup is generation-aware; Tests: restart-safe cache and session eviction).

## 4. Deployment and verification

- [x] 4.1 Configure two collision-free Aspire GameWorld resources and document the production one-serving-instance/restart/probe shape (Req: Deployment configuration enforces unique local world resources).
- [x] 4.2 Add focused MSTest coverage, run the targeted GameWorld/Contacts suites, validate OpenSpec, build affected projects, and review the final diff against all acceptance scenarios (all requirements).
- [x] 4.3 Configure Aspire GameWorld TCP endpoints as explicit proxyless target-port 443 endpoints and assert the DCP endpoint annotations instead of using a raw socket-only test (Req: Deployment configuration enforces unique local world resources; Test: proxyless Aspire TCP model).
