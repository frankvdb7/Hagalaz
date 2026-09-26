## 1. Bounded phase instrumentation

- [x] 1.1 Add per-batch cache-key, HybridCache outcome/factory, and returned-dictionary timing without IDs or per-definition activities; verify the data remains count/time/outcome-only in the source diff.
- [x] 1.2 Add repository, provider/archive, codec/fallback, and composition aggregate timings; verify activities remain nested under the existing region-load activity and existing resolution behavior is unchanged.

## 2. Validation and runtime handoff

- [x] 2.1 Run focused cached-service and MapRegionLoader tests, the full GameWorld test project, the GameWorld build, and `git diff --check`; record exact results (5/5, 13/13, 1,178/1,178; Release build 0 warnings/errors; diff check clean).
- [ ] 2.2 After the user restarts Aspire and reproduces one login, analyze the new bounded spans and report phase attribution without implementing an optimization.
