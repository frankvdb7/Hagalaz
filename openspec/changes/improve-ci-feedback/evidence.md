# Investigation evidence

## Benchmark reports

The [PR #589 run](https://github.com/frankvdb7/Hagalaz/actions/runs/43766499105/job/132972776796) reported 86 executed cases. Its uploaded artifact contains ten `*-report-full-compressed.json` files totaling 86 cases, but the merge selected only `*-report-full.json`, which exists for the 58-case `HagalazBenchmarks` class. The merged report and latest `gh-pages` baseline therefore contain 58 cases, not the complete suite.

Both `MapAppearanceRequestBenchmarks` cases had null statistics. Their setup threw `NotSupportedException: Old-layout models are not supported`; the log says no workload results were obtained. Counting executed cases is not evidence of successful measurements. The workflow now merges compressed reports and refuses to publish or compare missing statistics.

The fixture now uses a modern-layout triangle already exercised by `ModelTypeCodecTests`. A local Windows BenchmarkDotNet Dry run produced statistics for both appearance cases and passed the setup's 32-key response-equivalence checks. A hosted full run remains necessary to confirm the repaired suite and publication.

The log also records a tracked `Hagalaz.Game.Common.Tests.csproj` blocking `git switch gh-pages` but contains no diff. A local Windows dry run did not reproduce it. A diagnostic step records status and that file's diff on a future hosted run.

Per-class elapsed times in that run, excluding initial generation/build: archive LRU 17.8s, object workload 29.7s, reference memoization 19.7s, concurrent read 22.7s, member read 25.7s, shared HagalazBenchmarks 545.0s, Huffman 24.1s, map appearance 1.2s (failed), map terrain 18.1s, process states 1.6s. These are one sample and cannot establish a safe fast lane.

As observed on 2026-11-16, the main ruleset API reported CodeQL scanning requirements and no required status-check rule. The latest published benchmark history had 106 entries and 58 cases in its latest entry. This is point-in-time evidence.

## CodeQL

For [#589](https://github.com/frankvdb7/Hagalaz/actions/runs/43766499164/job/132972689485), [#585](https://github.com/frankvdb7/Hagalaz/actions/runs/43751748890/job/132927020893), and [#588](https://github.com/frankvdb7/Hagalaz/actions/runs/43762993184/job/132961740912), initialization took 12s, 14s, and 12s. The extraction-to-query group boundary took approximately 224s, 227s, and 223s; query-to-analysis diagnostic boundary took approximately 97s, 101s, and 101s. These boundaries include finalization and other work, not just individual phases. The current configuration scans C# in `none` mode and uses `security-and-quality`, with the existing single query exclusion. More detailed cache and extraction evidence remains to be collected.

[GitHub's guidance](https://docs.github.com/en/code-security/reference/code-scanning/troubleshoot-analysis-errors/analysis-takes-too-long) suggests larger runners or reducing/splitting analysis; the language matrix is already parallel. No change to coverage or runner size is justified by these samples. A manual build has not been profiled and is not assumed faster.
