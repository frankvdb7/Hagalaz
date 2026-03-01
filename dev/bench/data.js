window.BENCHMARK_DATA = {
  "lastUpdate": 1772385659117,
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
      },
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
          "id": "353e1c38ab8ac5b1921c637271578aebfc6b36b8",
          "message": "Solve build warnings and add comprehensive unit tests (#219)\n\n* Solve build warnings and add comprehensive unit tests\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Solve 1300+ build warnings and add comprehensive unit tests\n\n- Addressed nullability (CS8618), logging (CA2017), and stream read (CA2022) warnings.\n- Fixed ScaleX/ScaleY initialization bug in ItemType constructor.\n- Created Hagalaz.Services.Extensions.Tests, Hagalaz.Services.JagGrab.Tests, and Hagalaz.Models.Tests.\n- Improved FileStoreTests with multi-sector/large file coverage while maintaining all original edge-case tests.\n- Restored detailed round-trip validation in ItemTypeCodecTests for all properties and special item types (Noted/Lent).\n- Cleaned up all temporary fixup scripts.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Refactor to solve 1300+ build warnings and enhance test coverage.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-02-22T17:08:17+01:00",
          "tree_id": "d9ad62188e8bc56ed3923f180bec1be2268e40c4",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/353e1c38ab8ac5b1921c637271578aebfc6b36b8"
        },
        "date": 1771776702314,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.299241048949105,
            "unit": "ns",
            "range": "± 0.026417086788566718"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1734209278455148,
            "unit": "ns",
            "range": "± 0.008427001969065612"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2137.8751981099444,
            "unit": "ns",
            "range": "± 4.6778020399604054"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 886.0024087429047,
            "unit": "ns",
            "range": "± 0.5291524094531486"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 55.21378855063365,
            "unit": "ns",
            "range": "± 0.16958382063338598"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.153968974451224,
            "unit": "ns",
            "range": "± 0.008650945477289687"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10454.511009803186,
            "unit": "ns",
            "range": "± 36.50648599470017"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 994.6185911723545,
            "unit": "ns",
            "range": "± 0.9091091809014881"
          }
        ]
      },
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
          "id": "c987b31ebeb0478aaca24c2fdeaa6f78133cbf6f",
          "message": "Fix over 100 build and test warnings (#221)\n\n* Fix over 100 MSTest and C# warnings in tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Fix over 100 MSTest and C# warnings, resolving duplicates and obsolete attributes.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-02-22T18:42:08+01:00",
          "tree_id": "35c095d4a5583f192c5431f6b8a7130949a60709",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/c987b31ebeb0478aaca24c2fdeaa6f78133cbf6f"
        },
        "date": 1771782333779,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.293531661996475,
            "unit": "ns",
            "range": "± 0.031536033380558465"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1726870665947597,
            "unit": "ns",
            "range": "± 0.004432785387177878"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2139.7971357618057,
            "unit": "ns",
            "range": "± 7.120113092491038"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 965.4299606595721,
            "unit": "ns",
            "range": "± 1.3807070265643338"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 55.228147561733536,
            "unit": "ns",
            "range": "± 0.14908973768605924"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1570984021975446,
            "unit": "ns",
            "range": "± 0.00523472037273858"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10438.407603337215,
            "unit": "ns",
            "range": "± 80.68131098987203"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 995.8553454535348,
            "unit": "ns",
            "range": "± 0.9327442387874466"
          }
        ]
      },
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
          "id": "39a1ccf94df141605144f4bc160fb50f27ea1fd2",
          "message": "fix(web): improve UserStore loading state and error handling (#223)\n\n* fix(web): improve UserStore loading state and error handling\n\n- Add tap operator to set loading state before getUserInfo request.\n- Move timeout operator before tapResponse to ensure timeout errors are captured.\n- Reset error state when starting a new request.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* fix(web): improve UserStore loading state and error handling\n\n- Add tap operator to set loading state before getUserInfo request.\n- Move timeout operator before tapResponse to ensure timeout errors are captured.\n- Reset error state when starting a new request.\n- Add unit tests for UserStore to verify loading state and error handling.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-02-28T12:08:13+01:00",
          "tree_id": "c56bbb7f6cefc67f0e121e1071e0d1343cb7fd26",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/39a1ccf94df141605144f4bc160fb50f27ea1fd2"
        },
        "date": 1772277114839,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.29789285820264,
            "unit": "ns",
            "range": "± 0.012372841089064167"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.182888856955937,
            "unit": "ns",
            "range": "± 0.005752365369150809"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2140.7284460801343,
            "unit": "ns",
            "range": "± 2.3247780173112846"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 886.8590094021389,
            "unit": "ns",
            "range": "± 1.5022655298873488"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 60.083943183117725,
            "unit": "ns",
            "range": "± 2.407549022539896"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1315642802843024,
            "unit": "ns",
            "range": "± 0.004677032288809388"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10420.452214922223,
            "unit": "ns",
            "range": "± 68.8574792616731"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 995.3409649985177,
            "unit": "ns",
            "range": "± 0.7263965492949517"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "5500e9f11691ad88ab480be25dc835875622dcd5",
          "message": "build(deps): Bump the npm_and_yarn group across 1 directory with 2 updates (#220)\n\nBumps the npm_and_yarn group with 2 updates in the /Hagalaz.Web.App directory: [minimatch](https://github.com/isaacs/minimatch) and [qs](https://github.com/ljharb/qs).\n\n\nUpdates `minimatch` from 3.1.2 to 3.1.3\n- [Changelog](https://github.com/isaacs/minimatch/blob/main/changelog.md)\n- [Commits](https://github.com/isaacs/minimatch/compare/v3.1.2...v3.1.3)\n\nUpdates `qs` from 6.14.1 to 6.14.2\n- [Changelog](https://github.com/ljharb/qs/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/ljharb/qs/compare/v6.14.1...v6.14.2)\n\n---\nupdated-dependencies:\n- dependency-name: minimatch\n  dependency-version: 3.1.3\n  dependency-type: indirect\n  dependency-group: npm_and_yarn\n- dependency-name: qs\n  dependency-version: 6.14.2\n  dependency-type: indirect\n  dependency-group: npm_and_yarn\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Frank <5363672+frankvdb7@users.noreply.github.com>",
          "timestamp": "2026-02-28T12:08:57+01:00",
          "tree_id": "a768c44023aa1d81fa6a55b7c952590359b0db9f",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/5500e9f11691ad88ab480be25dc835875622dcd5"
        },
        "date": 1772277130127,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.332071124513943,
            "unit": "ns",
            "range": "± 0.06549725960447182"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1847973306264197,
            "unit": "ns",
            "range": "± 0.0072858194449754105"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2140.585588773092,
            "unit": "ns",
            "range": "± 1.8977810354286981"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 886.2065582275391,
            "unit": "ns",
            "range": "± 0.8596891335193944"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 58.570141177911026,
            "unit": "ns",
            "range": "± 0.19725634940628767"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.5900357529520988,
            "unit": "ns",
            "range": "± 0.007658626685790747"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10468.960580444336,
            "unit": "ns",
            "range": "± 14.969704513983151"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 997.2788118634905,
            "unit": "ns",
            "range": "± 1.5493275286145225"
          }
        ]
      },
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
          "id": "f6886a24291c5ff6384f9d2d9f6edfe2fd0076c0",
          "message": "⚡ Bolt: Optimize string parsing utilities with ReadOnlySpan (#226)\n\n* ⚡ Bolt: Optimize string parsing utilities with ReadOnlySpan\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored common string parsing utilities in `Hagalaz.Utilities/StringUtilities.cs` (`SelectIntFromString`, `SelectDoubleFromString`, `DecodeValues`) to use `ReadOnlySpan<char>` and manual `IndexOf` loops instead of `string.Split` and intermediate substring allocations.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which allocates an array of strings even for small inputs. These utilities are frequently used in AutoMapper profiles and core game logic, making them a high-impact target for reducing transient heap pressure and improving cache locality.\n\n📊 Impact:\n- Eliminated `string[]` and `string` allocations for segments in `SelectIntFromString` and `SelectDoubleFromString`.\n- Optimized `DecodeValues` to pre-count segments and allocate the result array exactly once.\n- Maintained lazy evaluation (yield return) compatibility while leveraging span-based parsing.\n- Measurable reduction in Gen 0 GC pressure for CSV parsing scenarios.\n\n🔬 Measurement:\nAdded `StringParsingBenchmarks` to the `Hagalaz.Benchmarks` project (while preserving existing benchmarks).\nResults for N=100:\n- `DecodeBoolValues`: 128 B allocated (down from ~2.5KB+ estimated for Split)\n- `SelectIntFromString`: 1232 B allocated (iterator state machine overhead, but 0 segment string allocations)\n\nVerified with 111 unit tests in `Hagalaz.Utilities.Tests`.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored `Hagalaz.Utilities/StringUtilities.cs` to eliminate transient string allocations during common CSV/separated-value parsing tasks.\n- Introduced `SpanValueParser<T>` delegate and `DecodeValuesFromSpan<T>` overload for truly allocation-free parsing of numeric segments.\n- Optimized `SelectIntFromString` and `SelectDoubleFromString` to use lazy evaluation via `yield return` while leveraging `ReadOnlySpan<char>` and manual `IndexOf` loops to avoid `string.Split` and segment string creation.\n- Enhanced `DecodeValues(string data)` (bool array) to be allocation-free (except for the final result array).\n- Added `CountSegments` helper to pre-allocate result arrays with exact capacity.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which creates a `string[]` and a new `string` object for every segment. These utilities are used heavily in AutoMapper profiles for character statistics and world data, creating significant GC pressure on the hot path.\n\n📊 Impact:\n- **Zero segment string allocations** for numeric and boolean parsing.\n- Exact array pre-allocation for eager parsing methods.\n- Maintained lazy evaluation for enumerable parsing.\n\n🔬 Measurement:\nUpdated `StringParsingBenchmarks`.\nFor N=100:\n- `DecodeBoolValues`: 128 B allocated (down from ~2.5KB+ estimated for original)\n- `DecodeIntValues_SpanDelegate`: 456 B allocated (down from ~3.5KB+ for string delegate)\n- `SelectIntFromString`: 1232 B allocated (iterator overhead only, 0 segment strings)\n\nVerified with 111 unit tests in `Hagalaz.Utilities.Tests`.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored `Hagalaz.Utilities/StringUtilities.cs` to eliminate transient string allocations during common CSV/separated-value parsing tasks.\n- Introduced `SpanValueParser<T>` delegate and `DecodeValuesFromSpan<T>` overload for truly allocation-free parsing of numeric segments.\n- Optimized `SelectIntFromString` and `SelectDoubleFromString` to use lazy evaluation via `yield return` while leveraging `ReadOnlySpan<char>` and manual `IndexOf` loops to avoid `string.Split` and segment string creation.\n- Enhanced `DecodeValues(string data)` (bool array) to be allocation-free (except for the final result array).\n- Added `CountSegments` helper to pre-allocate result arrays with exact capacity.\n- Integrated new benchmarks into the existing `HagalazBenchmarks` suite to ensure CI compatibility.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which creates a `string[]` and a new `string` object for every segment. These utilities are used heavily in AutoMapper profiles for character statistics and world data, creating significant GC pressure on the hot path.\n\n📊 Impact:\n- **~9x reduction in heap allocations** for eager numeric array parsing (e.g., `DecodeValues`).\n- **Zero segment string allocations** for numeric and boolean parsing.\n- Exact array pre-allocation for eager parsing methods.\n- Maintained lazy evaluation for enumerable parsing.\n\n🔬 Measurement:\nBenchmark results for N=1000:\n- `DecodeIntValues_StringDelegate`: 35.9 KB allocated\n- `DecodeIntValues_SpanDelegate`: 4.0 KB allocated (result array only)\n- `DecodeBoolValues`: 1024 B allocated (result array only)\n\nVerified with 111 unit tests in `Hagalaz.Utilities.Tests`.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing with SpanValueParser\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored `Hagalaz.Utilities/StringUtilities.cs` to eliminate transient string allocations during common separated-value parsing tasks.\n- Introduced `SpanValueParser<T>` delegate and `DecodeValues<T>` overload for allocation-free parsing of numeric segments.\n- Optimized `SelectIntFromString` and `SelectDoubleFromString` to use lazy evaluation via `yield return` while leveraging `ReadOnlySpan<char>` to avoid `string.Split` and segment string creation.\n- Enhanced `DecodeValues(string data)` (bool array) to be allocation-free (except for the result array).\n- Integrated all string parsing benchmarks into the existing `HagalazBenchmarks` suite to ensure CI compatibility.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which creates a `string[]` and a new `string` object for every segment. This created significant GC pressure on hot paths like character statistics loading.\n\n📊 Impact:\n- **Zero segment string allocations** for common numeric parsing.\n- Optimized result array pre-allocation using a manual delimiter count.\n- Maintained lazy evaluation for Enumerable parsing.\n\n🔬 Measurement:\nBenchmark results for N=1000:\n- `DecodeIntValues_SpanDelegate`: 4.0 KB allocated (result array only).\n- `DecodeIntValues_StringDelegate`: 35.9 KB allocated (original behavior).\n- `DecodeBoolValues`: 1024 B allocated (result array only).\n\nVerified with 111 unit tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored `Hagalaz.Utilities/StringUtilities.cs` to eliminate transient string allocations during common separated-value parsing tasks.\n- Introduced `SpanValueParser<T>` delegate and `DecodeValues<T>` overload for allocation-free parsing of numeric segments.\n- Optimized `SelectIntFromString` and `SelectDoubleFromString` to use lazy evaluation via `yield return` while leveraging `ReadOnlySpan<char>` to avoid `string.Split` and segment string creation.\n- Enhanced `DecodeValues(string data)` (bool array) to be allocation-free (except for the final result array).\n- Unified all performance benchmarks into the existing `HagalazBenchmarks` suite to ensure CI compatibility with the performance tracking action.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which creates a `string[]` and a new `string` object for every segment. This created significant GC pressure on hot paths like character statistics loading.\n\n📊 Impact:\n- **Zero segment string allocations** for common numeric parsing.\n- Optimized result array pre-allocation using a manual delimiter count.\n- Maintained lazy evaluation for Enumerable parsing.\n\n🔬 Measurement:\nBenchmark results for N=1000:\n- `DecodeIntValues_SpanDelegate`: 4.0 KB allocated (result array only).\n- `DecodeIntValues_StringDelegate`: 35.9 KB allocated (original behavior).\n- `DecodeBoolValues`: 1024 B allocated (result array only).\n\nVerified with 111 unit tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Zero-allocation string parsing utilities with ReadOnlySpan\n\n💡 What:\nRefactored `Hagalaz.Utilities/StringUtilities.cs` to eliminate transient string allocations during common separated-value parsing tasks.\n- Introduced `SpanValueParser<T>` delegate and `DecodeValues<T>` overload for allocation-free parsing of numeric segments.\n- Optimized `SelectIntFromString` and `SelectDoubleFromString` to use lazy evaluation via `yield return` while leveraging `ReadOnlySpan<char>` to avoid `string.Split` and segment string creation.\n- Enhanced `DecodeValues(string data)` (bool array) to be allocation-free (except for the final result array).\n- Unified all performance benchmarks into the existing `HagalazBenchmarks` suite to ensure CI compatibility.\n\n🎯 Why:\nThe previous implementation relied on `string.Split`, which creates a `string[]` and a new `string` object for every segment. This created significant GC pressure on hot paths like character statistics loading.\n\n📊 Impact:\n- **Zero segment string allocations** for common numeric parsing.\n- Optimized result array pre-allocation using a manual delimiter count.\n- Maintained lazy evaluation for Enumerable parsing.\n\n🔬 Measurement:\nBenchmark results for N=1000:\n- `DecodeIntValues_SpanDelegate`: 4.0 KB allocated (result array only).\n- `DecodeIntValues_StringDelegate`: 35.9 KB allocated (original behavior).\n- `DecodeBoolValues`: 1024 B allocated (result array only).\n\nVerified with 111 unit tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize string parsing utilities with ReadOnlySpan<char>\n\n- Refactored `SelectIntFromString`, `SelectDoubleFromString`, and `DecodeValues` to use `ReadOnlySpan<char>` and manual `IndexOf` loops.\n- Eliminated `string.Split` and intermediate substring allocations during CSV/separated-value parsing.\n- Introduced `SpanValueParser<T>` and `DecodeValuesFromSpan<T>` for zero-allocation parsing paths.\n- Maintained lazy evaluation for `IEnumerable` methods while bypassing ref struct yield limitations.\n- Reduced heap allocations by ~9x for numeric CSV parsing (N=1000: ~36KB down to ~4KB).\n- Unified parsing benchmarks in `Hagalaz.Benchmarks` and updated MSTest suite.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize string parsing utilities and fix CI benchmarks\n\n- Refactored `SelectIntFromString`, `SelectDoubleFromString`, and `DecodeValues` to use `ReadOnlySpan<char>` and manual `IndexOf` loops, eliminating `string.Split` and substring allocations.\n- Introduced `SpanValueParser<T>` and `DecodeValuesFromSpan<T>` for zero-allocation parsing paths.\n- Reduced heap allocations by ~9x for numeric CSV parsing (N=1000: ~36KB down to ~4KB).\n- Unified parsing benchmarks in `Hagalaz.Benchmarks`.\n- Fixed CI benchmark job in `.github/workflows/performance.yml` by removing unsupported `--toolchain` and `--exporter` options and using valid `--exporters json`.\n- Updated MSTest suite in `Hagalaz.Utilities.Tests` to resolve compiler warnings and naming collisions.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-01T17:17:20+01:00",
          "tree_id": "f0920beb05580073ad6dd951aff320300697febc",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/f6886a24291c5ff6384f9d2d9f6edfe2fd0076c0"
        },
        "date": 1772381887216,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 779195,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 437301,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 899096,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 547916,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1657433,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 1030941,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1721391,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1159259,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 804883,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 467116,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 929813,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 570387,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1238726,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 1084571,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1329425,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1265475,
            "unit": "ns",
            "range": "± 0"
          }
        ]
      },
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
          "id": "7ec7f0c6b393b023d3b7e426f2de5d7454c17c19",
          "message": "⚡ Bolt: Optimize StringUtilities.EncodeValues methods (#227)\n\n- Refactored EncodeValues<T> to use string.Join for better efficiency.\n- Refactored EncodeValues(bool[]) to use string.Create for zero-allocation intermediate buffers and exact sizing.\n- Added benchmarks for both methods in HagalazBenchmarks.\n\nPerformance Impact (N=1000):\n- EncodeIntValues: 24.792 us (was 35.494 us, ~30% improvement), 7.83 KB (was 61.56 KB, ~87% reduction)\n- EncodeBoolValues: 3.964 us (was 10.649 us, ~62% improvement), 4.02 KB (was 8.49 KB, ~52% reduction)\n\nVerified with 111 passing unit tests.\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-01T18:20:09+01:00",
          "tree_id": "8bb54c11b302a283976eb3d6bd48eb6ed3352da9",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/7ec7f0c6b393b023d3b7e426f2de5d7454c17c19"
        },
        "date": 1772385657734,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 843073,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 492449,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 946456,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 585353,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1185262,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 1030593,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1215739,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1114750,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 710947,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 838154,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 847151,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 517555,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 984747,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 602956,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1321285,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 1087419,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1291741,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1239814,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 720034,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 884891,
            "unit": "ns",
            "range": "± 0"
          }
        ]
      }
    ]
  }
}