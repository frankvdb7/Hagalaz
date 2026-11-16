# Investigation evidence

## Benchmark reports

The [PR #589 run](https://github.com/frankvdb7/Hagalaz/actions/runs/43766499105/job/132972776796) reported 86 executed cases. Its uploaded artifact contains ten `*-report-full-compressed.json` files totaling 86 cases, but the merge selected only `*-report-full.json`, which exists for the 58-case `HagalazBenchmarks` class. The merged report and latest `gh-pages` baseline therefore contain 58 cases, not the complete suite.

Both `MapAppearanceRequestBenchmarks` cases had null statistics. Their setup threw `NotSupportedException: Old-layout models are not supported`; the log says no workload results were obtained. Counting executed cases is not evidence of successful measurements. The workflow now merges compressed reports and refuses to publish or compare missing statistics.

The fixture now uses a modern-layout triangle already exercised by `ModelTypeCodecTests`. A local Windows BenchmarkDotNet Dry run produced statistics for both appearance cases and passed the setup's 32-key response-equivalence checks.

The [PR #595 hosted run](https://github.com/frankvdb7/Hagalaz/actions/runs/43936587868) produced and uploaded all 86 results with statistics, including both repaired appearance cases. The benchmark job took 13m41s and BenchmarkDotNet reported 12m51s. The tracked checkout was clean after the run, so the original dirty-file cause was not reproduced. The separate comparison job failed explicitly because the current main baseline lacks the additional cases; successful full-baseline comparison and main publication remain unverified. CI and all CodeQL language jobs passed for the same commit.

Main publish jobs use GitHub's `queue: max` concurrency setting to avoid concurrent writes while retaining pending runs. GitHub documents this setting, but actionlint 1.7.12 does not recognize it; linting with that specific unsupported-key diagnostic suppressed found no other workflow errors. GitHub queues jobs by arrival, not commit order, so the resulting history order needs hosted verification.

The log also records a tracked `Hagalaz.Game.Common.Tests.csproj` blocking `git switch gh-pages` but contains no diff. A local Windows dry run did not reproduce it. A diagnostic step records status and that file's diff on a future hosted run.

Per-class elapsed times in that run, excluding initial generation/build: archive LRU 17.8s, object workload 29.7s, reference memoization 19.7s, concurrent read 22.7s, member read 25.7s, shared HagalazBenchmarks 545.0s, Huffman 24.1s, map appearance 1.2s (failed), map terrain 18.1s, process states 1.6s. These are one sample and cannot establish a safe fast lane.

The shared 58-case class combines collections, cache file store, security, parsing, viewport and creature methods; its setup initializes multiple domains. Cache snapshot and map benchmarks also share fixtures and cache dependencies. Class-only filtering can therefore discard relevant cases, and a complete, maintainable method-level transitive impact map has not been demonstrated. The reduced lane is rejected for this change until that mapping and representative savings are measured; PRs retain the full filter. This decision does not claim a reduction in benchmark wall time or runner minutes.

As observed on 2026-11-16, the main ruleset API reported CodeQL scanning requirements and no required status-check rule. The latest published benchmark history had 106 entries and 58 cases in its latest entry. This is point-in-time evidence.

## CodeQL

For [#589](https://github.com/frankvdb7/Hagalaz/actions/runs/43766499164/job/132972689485), [#585](https://github.com/frankvdb7/Hagalaz/actions/runs/43751748890/job/132927020893), and [#588](https://github.com/frankvdb7/Hagalaz/actions/runs/43762993184/job/132961740912), initialization took 12s, 14s, and 12s. The extraction-to-query group boundary took approximately 224s, 227s, and 223s; query-to-analysis diagnostic boundary took approximately 97s, 101s, and 101s. These boundaries include finalization and other work, not just individual phases. The current configuration scans C# in `none` mode and uses `security-and-quality`, with the existing single query exclusion. More detailed cache and extraction evidence remains to be collected.

[GitHub's guidance](https://docs.github.com/en/code-security/reference/code-scanning/troubleshoot-analysis-errors/analysis-takes-too-long) suggests larger runners or reducing/splitting analysis; the language matrix is already parallel. Extraction itself took approximately 210s, 213s, and 209s, and finalization took 13s, 14s, and 14s. In the first run the extractor attempted isolated NuGet restores and logged failed restores for some packages, including EF Core dependencies. The log says it scans all C# in the working directory; generated-code and dependency resolution completeness were not independently established.

The advanced workflow does not enable [CodeQL dependency caching](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/manage-your-configuration/codeql-for-compiled-languages#enable-dependency-caching-for-codeql), which defaults to off. GitHub's action includes a C# none-mode cache behind a feature flag. A warm-cache comparison has not been made, so no saving or fix for the restore failures is claimed. No change to coverage, build mode, or runner size is justified by the samples.
