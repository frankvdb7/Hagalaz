# Tasks

## 1. Review

- [x] 1.1 Use the existing dialog to show the exact deterministic request diff, unsupported edits and static-cache and independent-commit wording. Prevent confirmation of a changed snapshot.
- [x] 1.2 Test exact values, ordering, mixed regions and kinds, cancellation and the match between reviewed and submitted edits.

## 2. Region keys

- [x] 2.1 Wire region-scoped key entry and the existing render key map through landscape save and reload. Show actionable errors for missing or unusable keys.
- [x] 2.2 Test correct request bodies, neighboring regions with different or no keys, invalid and wrong keys, and draft retention without an unkeyed fallback.

## 3. Recovery

- [x] 3.1 Track outcomes in the existing serial save flow. Reconcile confirmed region/kind saves against authoritative reads and preserve pending drafts, versions and retry safety.
- [x] 3.2 Test partial success across regions and within one region, reload and retry, unrelated edits, reload failure, 409 and post-write verification errors.
- [x] 3.3 Update the Cache Studio workflow documentation and run focused tests, the admin build and OpenSpec validation. Verify the confirmation and recovery UX in the editor.
