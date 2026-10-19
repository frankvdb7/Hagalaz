# Spec Delta

## MODIFIED Requirements

### Requirement: Automatic ordered transaction completion
Commit MUST perform automatic completion after unlock without a separate caller publication call. Fixed pre-publication domain completion MUST precede normal container publication and domain-owned batches MUST retain their existing failure policy. Container publishers MUST retain existing observable order independently of lock order; the first failure MUST skip later container publishers and all post-publication domain completion. Completion-owner order MUST be determined by the first occurrence of each owner while traversing resolved boundaries in participant encounter and contribution order, before lock sorting. Owners MUST process their own pending facts in FIFO order; no cross-owner mutation-interleaving guarantee is provided. Fixed post-publication completion MUST run only after normal container publication, with storage irreversible and locks released; its first failure MUST stop later owners. Completion facts MUST be discarded before boundary ownership is released. The transaction MUST NOT store executable domain callbacks or offer callback registration. Repository-owned mutation boundaries MUST invoke fixed owner completion stages. A single failure MUST preserve its original exception; multiple hook failures and an independent publication failure MUST survive as original leaf exceptions in one flat AggregateException. Validation determining mutation eligibility MUST remain before commit.

#### Scenario: Container publication fails
- **WHEN** a container publisher throws
- **THEN** later containers and all post-publication domain completion are skipped and storage stays committed

#### Scenario: Post-publication domain completion fails
- **WHEN** post-publication completion for one owner throws after all container publication
- **THEN** earlier completion remains observable, later owners are skipped, remaining facts are discarded, and storage stays committed

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an equipment-owned hook batch and normal publication both throw
- **THEN** required equipment hooks have been attempted and AggregateException retains both original failures

#### Scenario: Rollback discards owner completion
- **WHEN** equipment and pouch mutations record pending effects but the scope is disposed without commit
- **THEN** storage is restored, pending owner facts are discarded without observable effects, and a later unrelated scope executes none of those effects

#### Scenario: Completion owners follow first-seen participant order
- **WHEN** participants are supplied in an order different from lock acquisition and mutation order
- **THEN** each distinct completion owner completes once in the order of its first resolved boundary occurrence, independent of lock and mutation order

#### Scenario: One owner preserves its pending fact order
- **WHEN** one completion owner records multiple facts in a transaction
- **THEN** that owner completes the facts in the order they were recorded

#### Scenario: Aliased boundaries do not repeat an owner
- **WHEN** repeated or aliased participants resolve to boundaries associated with the same completion owner
- **THEN** that owner runs once per applicable completion phase

#### Scenario: Completion starts a disjoint scope
- **WHEN** post-unlock domain completion synchronously starts a new scope over storage not owned by the committing scope
- **THEN** the disjoint scope proceeds normally and pending completion facts remain associated with their originating transaction

#### Scenario: Completion cannot re-enter an overlapping scope
- **WHEN** committed completion attempts same-thread Begin or mutation against storage still owned by the committing scope
- **THEN** it throws `InvalidOperationException` and cannot consume or alter the committing scope's storage
