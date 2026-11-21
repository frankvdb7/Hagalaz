# Proposal

## Why

World and lobby sign-in, session lease renewal and deferred session cleanup have no health signal, so operators cannot see failed or slow sign-ins, lease loss versus transient claim-store errors, or cleanup that is stuck and retrying. Rate-limit rejections are already counted by `AuthenticationRateLimitingMetrics`; connection and reconnect events by `RaidoMetrics`.

**Goal:** the existing `Hagalaz.Services.GameWorld` meter reports sign-in outcomes and duration, lease renewal outcomes and outstanding deferred cleanup, without duplicating either existing owner.

## What Changes

- `hagalaz.gameworld.signin` counter (`{signin}`) and `hagalaz.gameworld.signin.duration` histogram (`s`), tags `flow=world|lobby` and `outcome=success|rejected|full|duplicate|failed|error|cancelled`. Recorded once by `AuthenticationService` at the outward `SignInLobbyAsync`/`SignInWorldAsync` boundary, not in nested admission. `rejected` covers invalid credentials, disabled and locked-out accounts; `duplicate` covers an already-logged-on account or taken session ownership; `failed` is any other unsuccessful result; `error` is a thrown exception (rethrown unchanged); `cancelled` is cancellation. A request rejected by the sign-in rate limiter is not recorded here.
- `hagalaz.gameworld.session.lease.renew` counter (`{renewal}`, tag `outcome=success|lost|error|cancelled`) in `GameSessionLeaseService`: one sample per real claim-renew attempt. `lost` means the claim store refused the renewal; `error` means the attempt threw (a transient failure is not a lost lease, even when the session is then aborted past its known deadline); `cancelled` means host shutdown. Sessions skipped while a world reservation is pending record nothing.
- `hagalaz.gameworld.session.cleanup.pending` gauge (`{session}`, tag `kind=claim_release|abort`): deferred cleanup still owned by `GameSessionStore`. It is maintained at the store's atomic add/remove transitions under its lock; an abort that is being processed or released for retry stays counted until removed.

No session, claim, account or IP identifiers are tags. Sign-in compensation, rollback, lease renewal and recovery behavior are unchanged.

## Non-goals

Raido transport reconnects and connection counts, circuit-breaker metrics (Polly), per-account or per-IP login analytics, changing admission behavior.
