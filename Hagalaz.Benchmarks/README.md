# Cache benchmarks

These BenchmarkDotNet cases exercise the production `CacheApi` and `ObjectTypeProvider` using deterministic in-memory cache files encoded by the real container, reference-table, archive, and object codecs. Fixture creation and cache warm-up run in global setup, outside the measured operation.

The baselines are side-by-side controls in each report:

- Single-member and concurrent-read cases compare snapshot reads with repeated `ReadArchive` reconstruction.
- Reference-table memoization compares the warm `CacheApi` lookup with direct `ReferenceTableProvider` decoding.
- LRU churn compares a two-archive snapshot capacity against archive reconstruction while cycling through three archives.
- The object workload compares warmed snapshots with a zero-capacity snapshot cache across a fixed 19,967-ID, 260-archive hot-set/long-tail workload using the real object provider and codec.

Run the cache suite in Release mode:

```sh
dotnet run --project Hagalaz.Benchmarks/Hagalaz.Benchmarks.csproj -c Release -- --filter "*Cache*Benchmarks*" --noOverwrite
```

Use `--job Dry` to validate the cases quickly or `--job Short` for a shorter comparison. The default BenchmarkDotNet job produces the standard statistical report and allocation columns.

## Map loading benchmarks

`MapTerrainDecodeBenchmarks` decodes the pinned revision-742 terrain and landscape payloads for region 12850 through the production C# codecs. Global setup checks the fixture hashes, four terrain planes and 5934 source placements. Each reported operation decodes one complete payload, including its input stream and output allocations. File loading and hash validation are outside the measured operation.

`MapAppearanceRequestBenchmarks` compares **32 individual appearance request delegates** with **one batch containing the same 32 unique keys**. Both use the actual registered cache-service handlers, request binding, scoped DI, cache reads/model decoding, placement assembly and ASP.NET Core response JSON serialization. Global setup verifies every result is successful and the batch appearances match the individual responses. Both methods return total serialized bytes. One reported operation means the complete 32-appearance workload, not one appearance; the baseline loop represents the real individual-request workload rather than artificial microbenchmark repetition.

The default appearance input is a deterministic, offline in-memory cache with 32 object definitions, four placement rotations and real legacy model bytecode from the existing regression fixture. It deliberately uses small models. Cache/reference metadata is warm after setup, and no artificial request delay is added. `[MemoryDiagnoser]` reports allocations and collection counts. The timed response sink counts bytes without retaining/growing a response buffer; setup captures the JSON separately for equivalence checks. These numbers include in-process request contexts/scopes and serializer buffers; they exclude routing middleware, authentication, sockets/TLS, network latency, disk I/O and browser rendering. They must not be reported as full HTTP throughput or cold-start timings.

Validate all four cases, then measure them:

```sh
dotnet run --project Hagalaz.Benchmarks/Hagalaz.Benchmarks.csproj -c Release -- --filter "Hagalaz.Benchmarks.Map*" --job Dry --noOverwrite
dotnet run --project Hagalaz.Benchmarks/Hagalaz.Benchmarks.csproj -c Release --no-build -- --filter "Hagalaz.Benchmarks.Map*" --noOverwrite
```

Use `--job Short` for a quick measurement and `--artifacts <directory>` to select an output directory. Read the generated `*-report-github.md` summaries. Dry jobs validate setup/execution only; their timings are not performance evidence. There is no machine-dependent pass/fail speed threshold.

### Optional real-cache appearance input

Set `HAGALAZ_BENCHMARK_CACHE_PATH` to a locally available cache directory. The appearance setup reads region 12850, chooses its first 32 distinct successfully renderable ground-plane appearance keys in source order and records their key-set SHA256 in the benchmark log. Failed/unsupported candidates are excluded during setup, and a cache with fewer than 32 successful keys fails setup. The measured single and batch workloads then use exactly those same keys through the same real C# handlers and file-store/cache configuration. Setup warms the shared reference/member caches; model reads/decoding/assembly and JSON serialization remain timed. No cache write endpoint is called.

PowerShell example:

```powershell
$env:HAGALAZ_BENCHMARK_CACHE_PATH = 'C:\cache742'
dotnet run --project Hagalaz.Benchmarks/Hagalaz.Benchmarks.csproj -c Release --no-build -- --filter '*MapAppearanceRequestBenchmarks*' --job Dry --noOverwrite
dotnet run --project Hagalaz.Benchmarks/Hagalaz.Benchmarks.csproj -c Release --no-build -- --filter '*MapAppearanceRequestBenchmarks*' --job Short --noOverwrite
Remove-Item Env:HAGALAZ_BENCHMARK_CACHE_PATH
```

Record the cache identity, selected-key hash, job/runtime and hardware with any published result. Compare methods within one dataset/run; the small offline fixture and selected real models are different workloads. The real-cache mode measures the filesystem/cache/assembly/serialization path, while the terrain codec cases continue to use the committed pinned payloads. It does not measure full region border/floor projection or all 840 ground-plane keys.

### Recorded local measurement (2026-11-04)

BenchmarkDotNet 0.15.8, Release, .NET SDK 10.0.403 / runtime 10.0.14, Windows 11, AMD Ryzen 7 7800X3D (8 cores / 16 logical processors). All measured cases ran out of process. Fixture and real-cache dry runs passed, including JSON equivalence and serialized-byte-count checks.

| Workload | Job | Mean | Standard deviation | Allocated per operation |
| --- | --- | --- | --- | --- |
| Decode region-12850 terrain | Default | 76.82 μs | 3.298 μs | 385.02 KB |
| Decode region-12850 landscape | Default | 42.93 μs | 1.360 μs | 406.52 KB |
| 32 individual real-cache appearance requests | ShortRun | 5.676 ms | 0.9522 ms | 5.20 MB |
| One batch of those 32 appearances | ShortRun | 4.540 ms | 0.2388 ms | 5.11 MB |

Appearance input: revision-742 cache from GameClient commit `2d6d99555c87d23d5909ff0f9dfea1e58fa5bdda`, `main_file_cache.dat2` SHA256 `0ec9aee124926979303af7e29a156d1f33138896153df6d4c0c4bb60078aa545`, selected-key SHA256 `447b6ed3969c8d718eabe7059dc8302a144bb8bbd35429c48d22c9310cfac8df`. Terrain/landscape use the pinned fixture hashes checked by setup.

The appearance ShortRun has only three measured iterations and wide timing uncertainty; it is exploratory, not proof of a stable speedup. Allocations are similar. These measurements isolate C# work and do not quantify batching's network benefit or end-to-end map loading. Rerun the default job on an idle machine before drawing performance conclusions.
