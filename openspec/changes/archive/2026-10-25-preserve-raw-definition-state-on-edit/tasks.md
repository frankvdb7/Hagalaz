# Tasks

## 1. Characterize raw object state

- [x] 1.1 Extend the Cache service HTTP integration fixture with a gateway definition and verify raw decode-encode-decode preserves omitted values while the existing name mutation exposes the persistence defect.

## 2. Preserve stored fields during edits

- [x] 2.1 Decode the mapped object member without gameplay hooks, validate the encoded replacement, persist it through `ReplaceArchiveMember`, and verify the focused integration regression passes.
