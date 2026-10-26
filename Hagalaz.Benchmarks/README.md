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
