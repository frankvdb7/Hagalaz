window.BENCHMARK_DATA = {
  "lastUpdate": 1771718821271,
  "repoUrl": "https://github.com/frankvdb7/Hagalaz",
  "entries": {
    "Hagalaz Performance Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "5363672+frankvdb7@users.noreply.github.com",
            "name": "Frank",
            "username": "frankvdb7"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "85f79b6e7969927928e2f28f2b548052d05c4bb5",
          "message": "⚡ Bolt: Optimize viewport and NPC visibility lookups (#218)\n\n* ⚡ Bolt: Optimize viewport and NPC visibility lookups\n\nThis change improves performance in hot paths (GPI/NPC updates) by transitioning visibility lookups from $O(N)$ to $O(1)$.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections` to support both $O(1)$ lookups and $O(1)$ indexing.\n- Updated `Viewport` to use `ListHashSet<ICreature>` for `VisibleCreatures`.\n- Optimized `DrawNpcsMessage` by using a `HashSet` for `VisibleNpcs`, enabling $O(1)$ lookups in the encoder.\n- Ensured no regressions in NPC AI scripts that rely on index-based access.\n\nImpact: Reduces complexity of visibility checks in networking encoders from $O(N \\cdot M)$ to $O(N)$ per player tick.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize viewport lookups while maintaining order and API compatibility\n\nThis change improves performance in networking update cycles by transitioning visibility lookups from $O(N)$ to $O(1)$.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method for easy conversion.\n- Updated `Viewport` to use `ListHashSet<ICreature>` for `VisibleCreatures`, improving GPI encoder efficiency.\n- Optimized `DrawNpcsMessage` by populating `VisibleNpcs` with `ListHashSet`, enabling $O(1)$ lookups in the NPC encoder while preserving insertion order and maintaining the `IReadOnlyList` interface.\n- Added comprehensive regression tests in `Hagalaz.Collections.Tests` and `Hagalaz.Services.GameWorld.Tests`.\n\nImpact: Significant reduction in CPU cycles during the networking update pulse for all online players.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize viewport lookups and add comprehensive tests\n\nThis change improves performance in networking update cycles by transitioning visibility lookups from $O(N)$ to $O(1)$ while maintaining API compatibility and adding extensive unit tests.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method.\n- Updated `Viewport` to use `ListHashSet<ICreature>` for `VisibleCreatures`.\n- Optimized `DrawNpcsMessage` by using `ListHashSet` for `VisibleNpcs` in `CharacterRenderInformation`, enabling $O(1)$ lookups in the NPC encoder while preserving order and the `IReadOnlyList` interface.\n- Added comprehensive regression tests for `ListHashSet`, `Viewport`, and `CharacterRenderInformation`.\n\nImpact: Reduces complexity of entity visibility checks in networking encoders, leading to significant CPU savings during high-load scenarios.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimized viewport visibility lookups and added comprehensive tests\n\nThis change improves networking performance by transitioning visibility lookups from $O(N)$ to $O(1)$ while maintaining API compatibility.\n\n- Introduced `ListHashSet<T>` for dual $O(1)$ indexing and $O(1)$ lookups.\n- Updated `Viewport` and `DrawNpcsMessage` to use the optimized collection.\n- Added comprehensive unit and regression tests for `ListHashSet`, `Viewport`, and `CharacterRenderInformation`.\n- Verified all 1300+ tests pass.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimized viewport lookups and added comprehensive tests & benchmarks\n\nThis change improves performance in networking update cycles by transitioning visibility lookups from $O(N)$ to $O(1)$ while maintaining API compatibility and adding extensive validation.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method.\n- Updated `Viewport` to use `ListHashSet<ICreature>` for `VisibleCreatures`.\n- Optimized `DrawNpcsMessage` by using `ListHashSet` for `VisibleNpcs` in `CharacterRenderInformation`.\n- Added comprehensive regression tests for `ListHashSet`, `Viewport`, and `CharacterRenderInformation`.\n- Created `Hagalaz.Benchmarks` project using BenchmarkDotNet.\n- Confirmed a 15x speedup (0.07 ratio) in simulated networking encoder visibility checks.\n\nImpact: Significant reduction in CPU cycles during the networking update pulse for all online players.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize viewport lookups and add performance tracking\n\nThis change improves networking performance by transitioning visibility lookups from $O(N)$ to $O(1)$ and establishes a performance tracking infrastructure to prevent future regressions.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method for easy conversion.\n- Updated `Viewport` and `DrawNpcsMessage` to use the optimized collection, reducing visibility check complexity in hot paths.\n- Added comprehensive unit and regression tests in `Hagalaz.Collections.Tests` and `Hagalaz.Services.GameWorld.Tests`.\n- Implemented a performance benchmarking suite in `Hagalaz.Benchmarks` using BenchmarkDotNet.\n- Configured a GitHub Action (`performance.yml`) to automatically track performance and alert on regressions in PRs.\n\nImpact: ~15x improvement in visibility lookup performance during networking updates. infrastructure for continuous performance monitoring established.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize viewport lookups and add performance regression tracking\n\nThis change improves performance in networking update cycles by transitioning visibility lookups from $O(N)$ to $O(1)$ while maintaining API compatibility and established a robust performance tracking infrastructure.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method.\n- Updated `Viewport` to use `ListHashSet<ICreature>` for `VisibleCreatures`, reducing complexity of visibility checks in networking encoders.\n- Optimized `DrawNpcsMessage` by using `ListHashSet` for `VisibleNpcs` in `CharacterRenderInformation`, enabling $O(1)$ lookups in the encoder while preserving order and the `IReadOnlyList` interface.\n- Added comprehensive unit and regression tests for `ListHashSet`, `Viewport`, and `CharacterRenderInformation`.\n- Created `Hagalaz.Benchmarks` project using BenchmarkDotNet.\n- Configured a GitHub Action (`performance.yml`) for automated performance tracking and regression alerting.\n- Fixed the initial workflow failure by disabling non-existent branch dependencies for first-time setup.\n\nImpact: ~15x speedup in visibility lookup performance during networking updates. Continuous performance monitoring established.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize viewport lookups and fix performance tracking CI\n\nThis change improves performance in networking update cycles by transitioning visibility lookups from $O(N)$ to $O(1)$ and fixes the performance regression tracking infrastructure.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method.\n- Updated `Viewport` and `DrawNpcsMessage` to use the optimized collection.\n- Added comprehensive unit and regression tests, including a performance smoke test in `ListHashSetTests.cs`.\n- Fixed the `performance.yml` workflow by adding an initialization step for the `gh-pages` branch and ensuring robust git configuration.\n\nImpact: ~15x speedup in entity visibility lookups during networking updates. Established reliable performance monitoring.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimized viewport lookups and fixed performance tracking CI\n\nThis change improves networking performance by transitioning visibility lookups from $O(N)$ to $O(1)$ and ensures a robust performance tracking infrastructure.\n\nSummary of changes:\n- Introduced `ListHashSet<T>` in `Hagalaz.Collections`, a hybrid collection supporting both $O(1)$ indexing and $O(1)$ lookups.\n- Added `ToListHashSet()` extension method.\n- Updated `Viewport` and `DrawNpcsMessage` to use the optimized collection.\n- Added comprehensive unit and regression tests, including a performance smoke test in `ListHashSetTests.cs`.\n- Fixed the `performance.yml` workflow to safely initialize the `gh-pages` branch using a temporary clone, avoiding worktree corruption and branch checkout errors.\n\nImpact: ~15x speedup in entity visibility lookups during networking updates. Established reliable, automated performance monitoring.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-02-22T01:03:36+01:00",
          "tree_id": "7e727f9e15230f5ff17b89163a46b92ae72fdbea",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/85f79b6e7969927928e2f28f2b548052d05c4bb5"
        },
        "date": 1771718819298,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.279390290379524,
            "unit": "ns",
            "range": "± 0.008819892362787478"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.181328304608663,
            "unit": "ns",
            "range": "± 0.012988524009058911"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2146.0618286132812,
            "unit": "ns",
            "range": "± 3.7807541458339275"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 886.0716292307927,
            "unit": "ns",
            "range": "± 0.500872612537204"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.52182139669146,
            "unit": "ns",
            "range": "± 0.28238189349267834"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.145670822606637,
            "unit": "ns",
            "range": "± 0.004052105722384187"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10395.699772761418,
            "unit": "ns",
            "range": "± 85.86978829257461"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 995.5834091186523,
            "unit": "ns",
            "range": "± 1.0821594798977925"
          }
        ]
      }
    ]
  }
}