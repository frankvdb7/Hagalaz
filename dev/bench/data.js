window.BENCHMARK_DATA = {
  "lastUpdate": 1774708041508,
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
          "id": "9dc29e6d243efb7d8a26a618a8e03fa614dc69f1",
          "message": "⚡ Bolt: Optimize ListHashSet capacity allocation (#228)\n\n* ⚡ Bolt: Optimize ListHashSet capacity allocation\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize ListHashSet capacity allocation with TryGetNonEnumeratedCount\n\nAdded a capacity constructor to ListHashSet<T> and updated the ToListHashSet<T> extension method to pre-allocate memory. Used Enumerable.TryGetNonEnumeratedCount for robust count detection from the source.\n\n💡 What: Implemented a capacity-aware constructor for the hybrid ListHashSet<T> collection and used it in the ToListHashSet extension method via TryGetNonEnumeratedCount.\n🎯 Why: Reduces memory allocations and CPU overhead caused by repeated resizing of internal collections during bulk data additions.\n📊 Impact: Consistently avoids multiple resizing cycles and associated GC pressure when the source count is identifiable.\n🔬 Measurement: Verified with benchmarks and all existing unit tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-01T19:29:12+01:00",
          "tree_id": "380fbf73d2310708aaf7cbef760d46c516e4ae4f",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/9dc29e6d243efb7d8a26a618a8e03fa614dc69f1"
        },
        "date": 1772389802551,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 884197,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 472500,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 1380233,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 565115,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1154733,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 991442,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1198339,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1086472,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 725573,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 949428,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 372479,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 942411,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 511033,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 991825,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 541275,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1361667,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 1181646,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1476010,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1302999,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 775267,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 879613,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 470893,
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
          "id": "24db7a774fd130dba494903375504692ce3635f9",
          "message": "Optimize HashHelper.ComputeHash and StringUtilities.CountSegments (#229)\n\n- Use static HashData methods in HashHelper to avoid algorithm instantiation and reduce allocations.\n- Use Convert.ToHexStringLower (available in .NET 9+) for zero-allocation hex string conversion.\n- Vectorize segment counting in StringUtilities using MemoryExtensions.Count.\n- Update benchmarks to track improvements.\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-01T22:31:36+01:00",
          "tree_id": "800000b65783d31be420c4e5b9d41c46611f427f",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/24db7a774fd130dba494903375504692ce3635f9"
        },
        "date": 1772400751504,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 773422,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 427472,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 858532,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 558940,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1124321,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 894389,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1107750,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1034141,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 646053,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 788771,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 346210,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 5506468,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 807156,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 470453,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 902835,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 516751,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1209762,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 983527,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1247171,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1119271,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 654119,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 809920,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 398989,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 5509304,
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
          "id": "c014ee0700a58b9ab89d2860b52e7829bac77193",
          "message": "⚡ Bolt: optimize viewport update and concurrentstore iteration (#230)\n\n* ⚡ Bolt: optimize viewport update and concurrentstore iteration\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: optimize viewport and refactor benchmark suite\n\n💡 What:\n- Optimized \\`Viewport.UpdateTick\\` by replacing Linq with manual loops and utilizing a pre-allocated \\`ListHashSet\\`.\n- Optimized \\`ConcurrentStore<TKey, TValue>\\` by implementing a non-boxing struct enumerator for \\`foreach\\` loops.\n- Refactored the monolithic \\`HagalazBenchmarks\\` class into logical partial classes: \\`Collections\\`, \\`Viewport\\`, \\`StringParsing\\`, and \\`Security\\`.\n\n🎯 Why:\n- \\`Viewport.UpdateTick\\` is a hot path executed every game tick for every player. Linq iterator allocations and delegate overhead were measurable.\n- \\`ConcurrentStore\\` iteration previously suffered from double boxing, increasing GC pressure.\n- \\`HagalazBenchmarks.cs\\` was becoming a monolithic file that was difficult to navigate and maintain.\n\n📊 Impact:\n- Viewport Update: ~42.6% speedup in benchmarks (18.84μs -> 10.81μs).\n- Viewport Update: ~43.5% reduction in initial allocation.\n- Improved code organization and maintainability of the benchmark suite without breaking existing CI reporting.\n\n🔬 Measurement:\n- Verified with BenchmarkDotNet; all 15 benchmarks discovered and functional.\n- All functional tests in \\`Hagalaz.Services.GameWorld.Tests\\` and \\`Hagalaz.Collections.Tests\\` passed.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: optimize viewport, refactor benchmarks, and reduce duplication\n\n💡 What:\n- Optimized \\`Viewport.UpdateTick\\` by replacing Linq with manual loops and utilizing a pre-allocated \\`ListHashSet\\`.\n- Optimized \\`ConcurrentStore<TKey, TValue>\\` by implementing a non-boxing struct enumerator for \\`foreach\\` loops.\n- Refactored the monolithic \\`HagalazBenchmarks\\` class into logical partial classes: \\`Collections\\`, \\`Viewport\\`, \\`StringParsing\\`, and \\`Security\\`.\n- Reduced code duplication in \\`MapRegion\\` and \\`Viewport\\` by extracting shared character and NPC processing logic into private helper methods (\\`ForEachCreature\\`, \\`ProcessVisibleCharacters\\`, etc.).\n\n🎯 Why:\n- Performance: \\`Viewport.UpdateTick\\` is a hot path. Linq and enumerator boxing were causing unnecessary allocations and CPU overhead.\n- Maintainability: Duplicated loops for characters and NPCs made the code harder to maintain and update.\n- Organization: \\`HagalazBenchmarks.cs\\` was becoming too large and difficult to navigate.\n\n📊 Impact:\n- Viewport Update: ~42.6% speedup in benchmarks (18.84μs -> 10.81μs).\n- Viewport Update: ~43.5% reduction in initial allocation.\n- Improved code reuse and readability across core game world components.\n\n🔬 Measurement:\n- Verified with BenchmarkDotNet; all 15 benchmarks discovered and functional.\n- All functional tests in \\`Hagalaz.Services.GameWorld.Tests\\` and \\`Hagalaz.Collections.Tests\\` passed.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-02T21:28:26+01:00",
          "tree_id": "49cf85c1703f83ce113b4fcd8f3344a077ccf1a8",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/c014ee0700a58b9ab89d2860b52e7829bac77193"
        },
        "date": 1772483373892,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 881374,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 478784,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 1500980,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 394937,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 5425033,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1264840,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 998403,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1201281,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1130029,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 741774,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 865124,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 912954,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 561898,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1201753,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 517796,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 864362,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 465529,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 1529844,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 439911,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 5495154,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1315855,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 1554150,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1254080,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1268787,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 776519,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 868801,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 936718,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 572830,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 1250143,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 532113,
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
          "id": "5282d95fd593694e1cd311ba55423bdbe12f3694",
          "message": "⚡ Bolt: Optimize Viewport Creature Visibility Tracking (#232)\n\n* ⚡ Bolt: optimize viewport creature visibility tracking\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: optimize viewport creature visibility tracking with typed collections\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-03T21:03:31+01:00",
          "tree_id": "69cb7abb094f95874ec7a6f2da16a63b38dafb42",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/5282d95fd593694e1cd311ba55423bdbe12f3694"
        },
        "date": 1772568266890,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 770652,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 442863,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 1287936,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 353843,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 4777021,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1032012,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 943784,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1007778,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 978861,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 605877,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 931285,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 888478,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 503343,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1526875,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 587447,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 1250169,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 345390,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 763062,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 387362,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 1324426,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 375499,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 4707074,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1109231,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 916236,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1080368,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1069629,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 626814,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 765873,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 876883,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 496869,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 1170143,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 452311,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 1374495,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 292887,
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
          "id": "72ab806d55a7a08d1437064c862f3035aa1be479",
          "message": "Update performance.yml (#235)\n\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>",
          "timestamp": "2026-03-04T22:40:53+01:00",
          "tree_id": "c93dbdb0d1f532f002f121951e053b6861e9f0ab",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/72ab806d55a7a08d1437064c862f3035aa1be479"
        },
        "date": 1772660795489,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.19331524769465,
            "unit": "ns",
            "range": "± 0.1317211094005412"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1847954988479614,
            "unit": "ns",
            "range": "± 0.009018142321915074"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 567.4049364725748,
            "unit": "ns",
            "range": "± 2.1337586389960332"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 979.3107347488403,
            "unit": "ns",
            "range": "± 14.357641290150479"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 636.6084229151407,
            "unit": "ns",
            "range": "± 4.999956450957333"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5006.918886820476,
            "unit": "ns",
            "range": "± 9.62412429859759"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4268.53475189209,
            "unit": "ns",
            "range": "± 59.62272047413776"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2165.740847269694,
            "unit": "ns",
            "range": "± 28.53165242093356"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4586.56138865153,
            "unit": "ns",
            "range": "± 14.026585878661164"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1315.0295969645183,
            "unit": "ns",
            "range": "± 7.286777933132664"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 136.52626585960388,
            "unit": "ns",
            "range": "± 1.900007729531929"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2129.4454053243003,
            "unit": "ns",
            "range": "± 5.481696128610639"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1005.8388086954752,
            "unit": "ns",
            "range": "± 0.6806603447472254"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1024.0613142649333,
            "unit": "ns",
            "range": "± 12.410039328489926"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 643.4884157180786,
            "unit": "ns",
            "range": "± 11.999210762439198"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3617.041645050049,
            "unit": "ns",
            "range": "± 19.293228479544712"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 58.866566161314644,
            "unit": "ns",
            "range": "± 0.1321539629330078"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1910965840021768,
            "unit": "ns",
            "range": "± 0.02619543018294632"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5840.188954671224,
            "unit": "ns",
            "range": "± 12.696549329765633"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9585.233098347982,
            "unit": "ns",
            "range": "± 68.40110773772068"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 740.9539372126261,
            "unit": "ns",
            "range": "± 1.5917997681736127"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 50709.594746907555,
            "unit": "ns",
            "range": "± 72.34648510148733"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 43949.60410563151,
            "unit": "ns",
            "range": "± 59.91380198258077"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22511.193216959637,
            "unit": "ns",
            "range": "± 302.4090438042367"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 47726.034830729164,
            "unit": "ns",
            "range": "± 124.3063491798293"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14706.979209899902,
            "unit": "ns",
            "range": "± 14.531771961478318"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1522.6457862854004,
            "unit": "ns",
            "range": "± 39.32227166887426"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 9720.478609720865,
            "unit": "ns",
            "range": "± 75.50453650150905"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 1018.9938513437907,
            "unit": "ns",
            "range": "± 2.189245529379954"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9169.183385213217,
            "unit": "ns",
            "range": "± 162.95859236466714"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5170.007047017415,
            "unit": "ns",
            "range": "± 98.33017197446638"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 30044.310272216797,
            "unit": "ns",
            "range": "± 285.6593820426786"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0,
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
          "id": "b68077a762beb154b48ef02f6ab8624f1bc03bc1",
          "message": "⚡ Bolt: Optimize Core Collections for Allocation-Free Iteration (#233)\n\n* ⚡ Bolt: Optimize Core Collections for Allocation-Free Iteration\n\nOptimized \\`ListHashSet<T>\\` and \\`ConcurrentStore<TKey, TValue>\\` to reduce memory allocations in hot paths:\n- Refactored \\`ListHashSet<T>.GetEnumerator()\\` to return \\`List<T>.Enumerator\\` directly, eliminating boxing of the enumerator during \\`foreach\\` loops (0B allocation per loop).\n- Added optimized \\`Any()\\` and \\`FirstOrDefault()\\` methods to \\`ConcurrentStore<TKey, TValue>\\` to avoid LINQ \\`IEnumerable\\` boxing and bypass \\`ConcurrentDictionary.Values\\` snapshotting.\n- Verified performance impact with benchmarks: \\`ListHashSet\\` iteration is now 100% allocation-free.\n- Ensured full test coverage for modified collection logic.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize Core Collections with Allocation-Free Iteration and Validation\n\nOptimized `ListHashSet<T>` and `ConcurrentStore<TKey, TValue>` for better performance and safety:\n- Refactored `ListHashSet<T>.GetEnumerator()` to return `List<T>.Enumerator` directly, eliminating boxing of the enumerator (0B allocation per loop).\n- Added optimized `Any()` and `FirstOrDefault()` methods to `ConcurrentStore<TKey, TValue>` that iterate over the dictionary directly to avoid LINQ overhead and snapshotting.\n- Implemented `ArgumentNullException` checks for predicates in `ConcurrentStore` to follow standard .NET conventions.\n- Verified performance impact with benchmarks and ensured full test coverage.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Update .jules/bolt.md\n\nCo-authored-by: gemini-code-assist[bot] <176961590+gemini-code-assist[bot]@users.noreply.github.com>\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize Core Collections with Allocation-Free Iteration and Validation\n\nOptimized \\`ListHashSet<T>\\` and \\`ConcurrentStore<TKey, TValue>\\` for better performance and safety:\n- Refactored \\`ListHashSet<T>.GetEnumerator()\\` to return \\`List<T>.Enumerator\\` directly, eliminating boxing of the enumerator (0B allocation per loop).\n- Added optimized \\`Any()\\` and \\`FirstOrDefault()\\` methods to \\`ConcurrentStore<TKey, TValue>\\` that iterate over the dictionary directly to avoid LINQ overhead and snapshotting.\n- Implemented \\`ArgumentNullException\\` checks for predicates in \\`ConcurrentStore\\` to follow standard .NET conventions.\n- Verified performance impact with benchmarks and ensured full test coverage.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>\nCo-authored-by: gemini-code-assist[bot] <176961590+gemini-code-assist[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-04T22:49:42+01:00",
          "tree_id": "e6ba22338fde49170f6bfffe71a5eda6918f60b1",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/b68077a762beb154b48ef02f6ab8624f1bc03bc1"
        },
        "date": 1772661041045,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 853542,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 455424,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 2201550,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 429666,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 388649,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 5500439,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 1201945,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 1002701,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 1156500,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 1131102,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 728506,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 845777,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 900188,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 594446,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1195202,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 482355,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 1506116,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 377738,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 836329,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 479901,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 1486588,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 502943,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 439584,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 5452201,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 1299165,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 1065247,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 1223894,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 1204578,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 727163,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 797605,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 1418770,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 583645,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 1272315,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 506239,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 1602053,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 327724,
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
          "id": "3f5159162f495b87c44b75d50e20a60780ee5ee0",
          "message": "feat: cache service and endpoints for Hagalaz.Cache (#234)",
          "timestamp": "2026-03-04T22:49:23+01:00",
          "tree_id": "5959e17d6d303d1b532de63f2e3a26852e8f49ef",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/3f5159162f495b87c44b75d50e20a60780ee5ee0"
        },
        "date": 1772661303259,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.13065385321776,
            "unit": "ns",
            "range": "± 0.0919313136706292"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.622164045770963,
            "unit": "ns",
            "range": "± 0.3807278066038154"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 568.162943204244,
            "unit": "ns",
            "range": "± 1.3660208818343962"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 981.4629421234131,
            "unit": "ns",
            "range": "± 4.26395741536709"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 641.9567209879557,
            "unit": "ns",
            "range": "± 2.831616757066889"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5036.939768473308,
            "unit": "ns",
            "range": "± 36.28756914244274"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4254.053212483724,
            "unit": "ns",
            "range": "± 36.2590981530899"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2183.9721552530923,
            "unit": "ns",
            "range": "± 12.034626846109783"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4553.634437561035,
            "unit": "ns",
            "range": "± 24.42433867483232"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1320.3119799296062,
            "unit": "ns",
            "range": "± 15.132688575521492"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 137.30849250157675,
            "unit": "ns",
            "range": "± 1.6314217040327097"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2133.083838144938,
            "unit": "ns",
            "range": "± 5.297187692876082"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1006.3404229482015,
            "unit": "ns",
            "range": "± 1.3094090593755152"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1056.4824441274006,
            "unit": "ns",
            "range": "± 7.3613164701569955"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 618.0469484329224,
            "unit": "ns",
            "range": "± 24.490834541859538"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3710.958054860433,
            "unit": "ns",
            "range": "± 37.85606411727899"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.004821541408697764,
            "unit": "ns",
            "range": "± 0.0066803786771402205"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.98896696170171,
            "unit": "ns",
            "range": "± 1.7077974871150707"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.225794586042563,
            "unit": "ns",
            "range": "± 0.04703289109152753"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5848.720184326172,
            "unit": "ns",
            "range": "± 18.633136022604543"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9644.861653645834,
            "unit": "ns",
            "range": "± 60.675971589374264"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 629.3019313812256,
            "unit": "ns",
            "range": "± 3.306081487122638"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 51252.729166666664,
            "unit": "ns",
            "range": "± 60.83233094312529"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 44034.80125935873,
            "unit": "ns",
            "range": "± 83.53529250647661"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22647.649358113606,
            "unit": "ns",
            "range": "± 162.09053871420107"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 47505.10298665365,
            "unit": "ns",
            "range": "± 59.78802954192357"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14719.509404500326,
            "unit": "ns",
            "range": "± 14.603300844140298"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1526.1491012573242,
            "unit": "ns",
            "range": "± 9.697516885533645"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10468.686569213867,
            "unit": "ns",
            "range": "± 9.496961213897361"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 995.6930084228516,
            "unit": "ns",
            "range": "± 2.185297449386537"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 8977.724156697592,
            "unit": "ns",
            "range": "± 97.12613881086212"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5087.314811706543,
            "unit": "ns",
            "range": "± 61.36374511137447"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 30614.313222249348,
            "unit": "ns",
            "range": "± 418.97267152114165"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.0025442298501729965,
            "unit": "ns",
            "range": "± 0.004406735366632982"
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
          "id": "cd01da44b8d9c9498459808b5d4daf7095d3490f",
          "message": "feat: admin web app (#236)\n\n* feat: admin web app\n\n* sync",
          "timestamp": "2026-03-05T18:59:27+01:00",
          "tree_id": "a5a0b76afb23774bb0d7e4f1305bfe94846a7b81",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/cd01da44b8d9c9498459808b5d4daf7095d3490f"
        },
        "date": 1772734101392,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.169242903590202,
            "unit": "ns",
            "range": "± 0.12137530302011279"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.174793449540933,
            "unit": "ns",
            "range": "± 0.029645058885684215"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 567.7863337198893,
            "unit": "ns",
            "range": "± 1.4513971389573026"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 70.31886605421703,
            "unit": "ns",
            "range": "± 1.529052538064406"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 992.7047627766927,
            "unit": "ns",
            "range": "± 9.645205331158271"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 629.871914545695,
            "unit": "ns",
            "range": "± 1.5158944695618997"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5000.49227142334,
            "unit": "ns",
            "range": "± 8.270749118933068"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4221.098129272461,
            "unit": "ns",
            "range": "± 31.16515543446646"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2217.5782457987466,
            "unit": "ns",
            "range": "± 16.309627099479258"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4513.620994567871,
            "unit": "ns",
            "range": "± 19.394084560773702"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1320.9609209696453,
            "unit": "ns",
            "range": "± 4.93496250036301"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 140.16571124394736,
            "unit": "ns",
            "range": "± 0.8775834929388958"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2134.7363090515137,
            "unit": "ns",
            "range": "± 7.01456555001685"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1012.7568213144938,
            "unit": "ns",
            "range": "± 0.20529111098709563"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1108.6718444824219,
            "unit": "ns",
            "range": "± 15.159586267445109"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 686.9742218653361,
            "unit": "ns",
            "range": "± 29.038503435466428"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 4322.530680338542,
            "unit": "ns",
            "range": "± 72.65328009231511"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.10253749291102092,
            "unit": "ns",
            "range": "± 0.17516121605020202"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 60.013347148895264,
            "unit": "ns",
            "range": "± 0.618737670567302"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.3039732898275056,
            "unit": "ns",
            "range": "± 0.012935904305427508"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5259.515469868978,
            "unit": "ns",
            "range": "± 15.228549977369234"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 628.2014357248942,
            "unit": "ns",
            "range": "± 0.08469396703066494"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9367.897852579752,
            "unit": "ns",
            "range": "± 217.58075089032295"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 631.4397141138712,
            "unit": "ns",
            "range": "± 6.055121891921454"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 51436.390950520836,
            "unit": "ns",
            "range": "± 121.46145681374202"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 44809.48786417643,
            "unit": "ns",
            "range": "± 126.29758113891056"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22289.568415323894,
            "unit": "ns",
            "range": "± 99.55746993572644"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 48201.712829589844,
            "unit": "ns",
            "range": "± 785.3188955690609"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14752.228225708008,
            "unit": "ns",
            "range": "± 38.76071959375746"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1457.0893777211506,
            "unit": "ns",
            "range": "± 45.48552222083447"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10453.776484171549,
            "unit": "ns",
            "range": "± 8.356367996779912"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 997.736208597819,
            "unit": "ns",
            "range": "± 0.6903178244241417"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9026.668095906576,
            "unit": "ns",
            "range": "± 119.33462893809694"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5120.036702473958,
            "unit": "ns",
            "range": "± 68.19040382324243"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 31245.8486328125,
            "unit": "ns",
            "range": "± 210.3595793244194"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0,
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
          "id": "fc44816b0493df783ea033fd9b2805f3dd141ea4",
          "message": "Update performance.yml (#237)\n\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>",
          "timestamp": "2026-03-05T18:52:32+01:00",
          "tree_id": "f6836b9c44f43fb18c42877e99c684ad04fc188c",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/fc44816b0493df783ea033fd9b2805f3dd141ea4"
        },
        "date": 1772734988266,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.04920794069767,
            "unit": "ns",
            "range": "± 0.016261116986759076"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.175812308986982,
            "unit": "ns",
            "range": "± 0.007669044452763894"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 568.0594787597656,
            "unit": "ns",
            "range": "± 2.470668556607301"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 69.43074224392574,
            "unit": "ns",
            "range": "± 1.6277033465720092"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 1009.0075022379557,
            "unit": "ns",
            "range": "± 3.644392685487884"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 641.9410285949707,
            "unit": "ns",
            "range": "± 6.3605547427654585"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 4996.916534423828,
            "unit": "ns",
            "range": "± 15.821034908580321"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4249.971239725749,
            "unit": "ns",
            "range": "± 12.709810326156251"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2423.244313557943,
            "unit": "ns",
            "range": "± 1.616061160565955"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4490.417093912761,
            "unit": "ns",
            "range": "± 8.332258775571674"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1321.6283219655354,
            "unit": "ns",
            "range": "± 2.19714963750241"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 157.02217439810434,
            "unit": "ns",
            "range": "± 2.045625350956105"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2139.168471018473,
            "unit": "ns",
            "range": "± 13.988673979445768"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1005.9134902954102,
            "unit": "ns",
            "range": "± 1.749894115264329"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1086.9077911376953,
            "unit": "ns",
            "range": "± 2.885204118431478"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 723.1373065312704,
            "unit": "ns",
            "range": "± 11.262111211257578"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3807.5115025838218,
            "unit": "ns",
            "range": "± 63.0126175964745"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.0012256999810536702,
            "unit": "ns",
            "range": "± 0.0021229746420211673"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.3481812675794,
            "unit": "ns",
            "range": "± 0.06899612048891161"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1628908241788545,
            "unit": "ns",
            "range": "± 0.003018323220261634"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5243.401852925618,
            "unit": "ns",
            "range": "± 10.437080021432088"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 628.4154609044393,
            "unit": "ns",
            "range": "± 0.6295731787676685"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9587.777725219727,
            "unit": "ns",
            "range": "± 47.73084231798584"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 642.8996334075928,
            "unit": "ns",
            "range": "± 3.6188428393388175"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 51336.02364095052,
            "unit": "ns",
            "range": "± 82.11318736101006"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 41783.44643147787,
            "unit": "ns",
            "range": "± 142.79686666905008"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22908.125762939453,
            "unit": "ns",
            "range": "± 144.29353265012963"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 47781.16979980469,
            "unit": "ns",
            "range": "± 115.36734857405557"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14752.582997639975,
            "unit": "ns",
            "range": "± 22.14241535359793"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1630.5884119669597,
            "unit": "ns",
            "range": "± 35.759556759595085"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 11354.083969116211,
            "unit": "ns",
            "range": "± 405.46825941302166"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 1071.8977025349934,
            "unit": "ns",
            "range": "± 2.4738103259614026"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9500.3525390625,
            "unit": "ns",
            "range": "± 17.403058807703722"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5402.543950398763,
            "unit": "ns",
            "range": "± 82.90996955755446"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 32259.965738932293,
            "unit": "ns",
            "range": "± 457.4988755768804"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.0016190782189369202,
            "unit": "ns",
            "range": "± 0.0016548993695310534"
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
          "id": "e4bb1e6597e0edbfec7bee15782a96d42f5ef2ec",
          "message": "chore: remove obsolete projects (#241)",
          "timestamp": "2026-03-07T17:13:07+01:00",
          "tree_id": "2247f9c2aa89e54b0a57169946197434e5a69889",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/e4bb1e6597e0edbfec7bee15782a96d42f5ef2ec"
        },
        "date": 1772900335275,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.050081779559454,
            "unit": "ns",
            "range": "± 0.01527244387112351"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.162227754791578,
            "unit": "ns",
            "range": "± 0.005965782257258428"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 566.763040860494,
            "unit": "ns",
            "range": "± 0.9191076827558656"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.29744124412537,
            "unit": "ns",
            "range": "± 0.033000515516587885"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 985.6807486216227,
            "unit": "ns",
            "range": "± 9.029798163847287"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 640.5157670974731,
            "unit": "ns",
            "range": "± 3.546172592908255"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 4940.735633850098,
            "unit": "ns",
            "range": "± 12.01736803758658"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4197.349833170573,
            "unit": "ns",
            "range": "± 30.568482240591393"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2182.2062950134277,
            "unit": "ns",
            "range": "± 12.479425283604032"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4659.573427836101,
            "unit": "ns",
            "range": "± 6.496938854453458"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1319.3342812856038,
            "unit": "ns",
            "range": "± 9.379607077110348"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 150.46428561210632,
            "unit": "ns",
            "range": "± 1.452598703841623"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2130.693400065104,
            "unit": "ns",
            "range": "± 8.994329246984934"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1009.2612234751383,
            "unit": "ns",
            "range": "± 2.6595064113819733"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1058.8572438557942,
            "unit": "ns",
            "range": "± 12.68160318022146"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 629.5391918818156,
            "unit": "ns",
            "range": "± 15.418078863944404"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3634.4428647359214,
            "unit": "ns",
            "range": "± 35.59983984153734"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.003966130316257477,
            "unit": "ns",
            "range": "± 0.006869539217197169"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 55.01969369252523,
            "unit": "ns",
            "range": "± 0.1856607564944106"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1889810090263686,
            "unit": "ns",
            "range": "± 0.007835367015170139"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5853.627446492513,
            "unit": "ns",
            "range": "± 41.74999154373539"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 628.6489572525024,
            "unit": "ns",
            "range": "± 0.5563190092660808"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9544.855575561523,
            "unit": "ns",
            "range": "± 71.99053361741838"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 736.3712288538615,
            "unit": "ns",
            "range": "± 1.6076710130198697"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 52795.47812906901,
            "unit": "ns",
            "range": "± 99.39827036493756"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 43860.296407063805,
            "unit": "ns",
            "range": "± 101.67959034502071"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22574.452458699543,
            "unit": "ns",
            "range": "± 83.34956140207483"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 47612.80327351888,
            "unit": "ns",
            "range": "± 317.2005697752305"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14789.766225179037,
            "unit": "ns",
            "range": "± 138.02364325973483"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1613.6880054473877,
            "unit": "ns",
            "range": "± 17.002644741427318"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10399.587882995605,
            "unit": "ns",
            "range": "± 138.653219777485"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 998.2541364034017,
            "unit": "ns",
            "range": "± 3.5818882189444845"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 8939.31194559733,
            "unit": "ns",
            "range": "± 48.97051673318393"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 4975.382428487142,
            "unit": "ns",
            "range": "± 45.874036511600494"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 31292.141194661457,
            "unit": "ns",
            "range": "± 491.5503938069046"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.000028349459171295166,
            "unit": "ns",
            "range": "± 0.000049102703651782707"
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
          "id": "98741937501923da0c63e712f18c5e522394b878",
          "message": "⚡ Bolt: Optimize FileStore memory and copying performance (#248)\n\n* ⚡ Bolt: Optimize FileStore memory and copying performance\n\n- Optimized FileStore.Read and FileStore.Write using ArrayPool<byte> and Span<byte>.\n- Added ReadOnlySpan<byte> overloads to ISectorCodec and BigEndianMemoryStreamExtensions to reduce allocations.\n- Fixed sector padding in SectorCodec using a static zero buffer.\n- Added comprehensive benchmarks for the cache system.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize FileStore memory and copying performance\n\n- Optimized FileStore.Read and FileStore.Write using ArrayPool<byte> and Span<byte> to eliminate redundant allocations.\n- Enhanced ISectorCodec and BigEndianMemoryStreamExtensions with ReadOnlySpan<byte> support to avoid intermediate array copies.\n- Fixed a bug where short sectors were being written without padding; implemented efficient padding using a static shared buffer.\n- Added a comprehensive benchmark suite for the cache system.\n- Added targeted unit tests for sector encoding and padding boundaries.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize FileStore memory and copying performance\n\n- Optimized FileStore.Read and FileStore.Write using ArrayPool<byte> and Span<byte> to eliminate redundant allocations.\n- Enhanced ISectorCodec and BigEndianMemoryStreamExtensions with ReadOnlySpan<byte> support to avoid intermediate array copies.\n- Fixed a bug where short sectors were being written without padding; implemented efficient padding using a static shared buffer.\n- Improved MemoryStream safety in FileStore.Read by setting publiclyVisible to false.\n- Added a comprehensive benchmark suite for the cache system.\n- Added targeted unit tests for sector encoding, padding boundaries, and exact sector size multiples.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Comprehensive FileStore memory and I/O optimization\n\n- Optimized FileStore.Read and FileStore.Write using ArrayPool<byte> and Span<byte> to eliminate per-iteration allocations.\n- Enhanced ISectorCodec with span-based Decode and Encode overloads, including direct write-to-span encoding.\n- Improved BigEndianMemoryStreamExtensions with ReadOnlySpan<byte> support.\n- Fixed sector padding in SectorCodec using a static zero buffer.\n- Restricted MemoryStream visibility in FileStore.Read to prevent unexpected modifications.\n- Added a full benchmark suite for cache operations in Hagalaz.Benchmarks.\n- Expanded Hagalaz.Cache.Tests with targeted unit tests for new overloads and boundary conditions (padding, sector multiples).\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Comprehensive FileStore memory and I/O optimization\n\n- Optimized FileStore.Read and FileStore.Write using ArrayPool<byte> and Span<byte> to eliminate per-iteration allocations.\n- Enhanced ISectorCodec with span-based Decode and Encode overloads, including direct write-to-span encoding.\n- Improved BigEndianMemoryStreamExtensions with ReadOnlySpan<byte> support.\n- Fixed sector padding in SectorCodec using a static zero buffer.\n- Restricted MemoryStream visibility in FileStore.Read to prevent unexpected modifications.\n- Added a full benchmark suite for cache operations in Hagalaz.Benchmarks.\n- Expanded Hagalaz.Cache.Tests with targeted unit tests for new overloads and boundary conditions (padding, sector multiples).\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-21T11:17:39+01:00",
          "tree_id": "e61b9dc15cd4a0f046af5ac9029120e042b06769",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/98741937501923da0c63e712f18c5e522394b878"
        },
        "date": 1774088672914,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.360007892052332,
            "unit": "ns",
            "range": "± 0.0020529667801330027"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1615998347600303,
            "unit": "ns",
            "range": "± 0.001792063200285557"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 569.4919401804606,
            "unit": "ns",
            "range": "± 5.712891595000744"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.03192508220673,
            "unit": "ns",
            "range": "± 0.04596265488413125"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 925.8765967686971,
            "unit": "ns",
            "range": "± 4.723059130073495"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 100)",
            "value": 2720.29318745931,
            "unit": "ns",
            "range": "± 6.109492420994015"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 100)",
            "value": 25885.280049641926,
            "unit": "ns",
            "range": "± 19.47217950534647"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 100)",
            "value": 551376.0065104166,
            "unit": "ns",
            "range": "± 10926.164827912642"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 100)",
            "value": 685719.3639322916,
            "unit": "ns",
            "range": "± 6422.904733058036"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 637.3217055002848,
            "unit": "ns",
            "range": "± 3.2837857942019597"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5006.910898844401,
            "unit": "ns",
            "range": "± 26.962365132769143"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4244.395024617513,
            "unit": "ns",
            "range": "± 17.137009876527387"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2116.3144925435386,
            "unit": "ns",
            "range": "± 13.43741472745674"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4427.578501383464,
            "unit": "ns",
            "range": "± 14.236834664537696"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1308.93390973409,
            "unit": "ns",
            "range": "± 4.148416904385635"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 137.92752885818481,
            "unit": "ns",
            "range": "± 1.5606299132116195"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2144.741970062256,
            "unit": "ns",
            "range": "± 10.179056826392026"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1012.9510803222656,
            "unit": "ns",
            "range": "± 1.9643217076855028"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1035.7908871968586,
            "unit": "ns",
            "range": "± 8.213204882760587"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 587.1561584472656,
            "unit": "ns",
            "range": "± 3.540863042132338"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3385.841018676758,
            "unit": "ns",
            "range": "± 3.6334175420914074"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.00879895687103,
            "unit": "ns",
            "range": "± 0.2960293136027121"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.178133783241113,
            "unit": "ns",
            "range": "± 0.008135030426967005"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5259.6607615153,
            "unit": "ns",
            "range": "± 14.814943112183778"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 628.9245255788168,
            "unit": "ns",
            "range": "± 1.2600008503278706"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 8984.190353393555,
            "unit": "ns",
            "range": "± 14.591928572260503"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 1000)",
            "value": 2747.6884638468423,
            "unit": "ns",
            "range": "± 22.670489284724262"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 1000)",
            "value": 25614.66535949707,
            "unit": "ns",
            "range": "± 86.30067152004274"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 1000)",
            "value": 548061.4993489584,
            "unit": "ns",
            "range": "± 5499.8841535757265"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 1000)",
            "value": 692608.1302083334,
            "unit": "ns",
            "range": "± 9909.745123747396"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 735.2166169484457,
            "unit": "ns",
            "range": "± 5.975333595086864"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 50164.706410725914,
            "unit": "ns",
            "range": "± 142.6329031327932"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 42096.32385253906,
            "unit": "ns",
            "range": "± 576.8425156887333"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 21395.605860392254,
            "unit": "ns",
            "range": "± 61.43165700812331"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 46941.89002482096,
            "unit": "ns",
            "range": "± 161.67789453682954"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14971.345570882162,
            "unit": "ns",
            "range": "± 32.8771309231964"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1205.576681137085,
            "unit": "ns",
            "range": "± 0.5464971447943203"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 9667.363149007162,
            "unit": "ns",
            "range": "± 55.33872084053733"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 986.4949073791504,
            "unit": "ns",
            "range": "± 3.198963800105242"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 8336.777572631836,
            "unit": "ns",
            "range": "± 35.489285866448846"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 4796.5403391520185,
            "unit": "ns",
            "range": "± 22.61997018663979"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 29076.958623250324,
            "unit": "ns",
            "range": "± 66.00953432219568"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.00040480246146519977,
            "unit": "ns",
            "range": "± 0.0006700339824094971"
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
          "id": "f3eda487418b1d2619c35fe52c92551c8273f797",
          "message": "⚡ Bolt: Optimize StringUtilities.GetStringInBetween (#244)\n\n* ⚡ Bolt: Optimize StringUtilities.GetStringInBetween\n\nThis commit refactors \\`GetStringInBetween\\` to use \\`ReadOnlySpan<char>\\`, eliminating intermediate \\`Substring\\` allocations.\n\nPerformance Impact (N=1000):\n- Execution Time: ~1,062 ns -> ~614 ns (~42% faster)\n- Memory Allocations: 4168 B -> 2112 B (~49% reduction)\n\nMeasurement:\n- Added a benchmark to \\`Hagalaz.Benchmarks/HagalazBenchmarks.StringParsing.cs\\`\n- Verified with \\`dotnet run -c Release --project Hagalaz.Benchmarks -- --filter *GetStringInBetween*\\`\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize StringUtilities.GetStringInBetween and fix logic bug\n\nThis commit optimizes \\`GetStringInBetween\\` using \\`ReadOnlySpan<char>\\` and fixes a bug where identical or overlapping markers would lead to incorrect results.\n\nChanges:\n- Refactored \\`GetStringInBetween\\` to use \\`ReadOnlySpan<char>\\` for O(1) slicing and zero intermediate allocations.\n- Fixed logic to always search for the end marker after the end of the begin marker.\n- Added a regression test case in \\`StringUtilitiesTests.cs\\` covering identical markers with \\`includeBegin=true\\`.\n- Updated benchmarks to reflect the performance gains while maintaining correctness.\n\nPerformance Impact (N=1000):\n- Execution Time: ~1,062 ns -> ~629 ns (~40% faster)\n- Memory Allocations: 4168 B -> 2112 B (~49% reduction)\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize StringUtilities.GetStringInBetween and fix logic bug\n\n- Refactored to use ReadOnlySpan<char> for zero-allocation slicing.\n- Fixed logic bug where identical start/end markers caused incorrect results when includeBegin was true.\n- Added regression test case.\n- Performance: ~40% faster, ~49% fewer allocations.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-21T11:18:22+01:00",
          "tree_id": "1df84442f350557c3c378f51ed9c277f44ced69a",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/f3eda487418b1d2619c35fe52c92551c8273f797"
        },
        "date": 1774088731165,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.31907077630361,
            "unit": "ns",
            "range": "± 0.08446802615768655"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.2132144471009574,
            "unit": "ns",
            "range": "± 0.02119362763141149"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 569.4171463648478,
            "unit": "ns",
            "range": "± 1.5104895729918342"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.8051428993543,
            "unit": "ns",
            "range": "± 0.0818507621003484"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 973.0075117746989,
            "unit": "ns",
            "range": "± 3.648576269386951"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 100)",
            "value": 2702.84596379598,
            "unit": "ns",
            "range": "± 10.329361762519872"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 100)",
            "value": 25871.97442626953,
            "unit": "ns",
            "range": "± 76.5793837912081"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 100)",
            "value": 1098298.4934895833,
            "unit": "ns",
            "range": "± 88505.66855911311"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 100)",
            "value": 1244356.4192708333,
            "unit": "ns",
            "range": "± 97566.41942621245"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 641.4607652028402,
            "unit": "ns",
            "range": "± 1.726800803556187"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 100)",
            "value": 55.12511303027471,
            "unit": "ns",
            "range": "± 0.6199691649761958"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5016.435190836589,
            "unit": "ns",
            "range": "± 10.802130110397101"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4226.615882873535,
            "unit": "ns",
            "range": "± 14.476478842815897"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2246.085393269857,
            "unit": "ns",
            "range": "± 16.112116938268013"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4688.387145996094,
            "unit": "ns",
            "range": "± 12.277173197209862"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1277.8284664154053,
            "unit": "ns",
            "range": "± 3.420691569195509"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 141.77368295192719,
            "unit": "ns",
            "range": "± 0.5825180971712604"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2138.5521125793457,
            "unit": "ns",
            "range": "± 2.6930360676768452"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1062.8769041697185,
            "unit": "ns",
            "range": "± 2.0195783202456346"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1064.1987291971843,
            "unit": "ns",
            "range": "± 15.118252724769157"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 632.8378864924113,
            "unit": "ns",
            "range": "± 21.207507073507504"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3566.7075640360513,
            "unit": "ns",
            "range": "± 31.28223564772669"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.0008257851004600525,
            "unit": "ns",
            "range": "± 0.0007642147182891271"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.40517381827036,
            "unit": "ns",
            "range": "± 2.031421299350829"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.2220621133844056,
            "unit": "ns",
            "range": "± 0.025234138775535712"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5840.717180887858,
            "unit": "ns",
            "range": "± 12.556710542762657"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 628.4830379486084,
            "unit": "ns",
            "range": "± 0.10812935749078771"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9307.850891113281,
            "unit": "ns",
            "range": "± 69.28977441204077"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 1000)",
            "value": 2718.0429153442383,
            "unit": "ns",
            "range": "± 9.877641770922361"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 1000)",
            "value": 25799.98533630371,
            "unit": "ns",
            "range": "± 81.11593854289607"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 1000)",
            "value": 1093739.4778645833,
            "unit": "ns",
            "range": "± 50331.606216260014"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 1000)",
            "value": 1205787.9348958333,
            "unit": "ns",
            "range": "± 138620.30175986252"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 647.2820154825846,
            "unit": "ns",
            "range": "± 3.960205220211066"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 1000)",
            "value": 204.72127103805542,
            "unit": "ns",
            "range": "± 6.136484472407059"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 53193.20650227865,
            "unit": "ns",
            "range": "± 94.1052656692537"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 41623.048583984375,
            "unit": "ns",
            "range": "± 123.70230108978771"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22537.931025187176,
            "unit": "ns",
            "range": "± 94.3419126659987"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 48034.335205078125,
            "unit": "ns",
            "range": "± 127.13628737777924"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14976.087290445963,
            "unit": "ns",
            "range": "± 29.05864630692423"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1859.4229049682617,
            "unit": "ns",
            "range": "± 91.40627796750854"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10102.562967936197,
            "unit": "ns",
            "range": "± 24.0012181128912"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 994.2420724232992,
            "unit": "ns",
            "range": "± 0.40464706536773976"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9199.583089192709,
            "unit": "ns",
            "range": "± 94.75776671916988"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5113.090970357259,
            "unit": "ns",
            "range": "± 102.63477035564695"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 32832.82154337565,
            "unit": "ns",
            "range": "± 390.106844947126"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.0034032675127188363,
            "unit": "ns",
            "range": "± 0.004937615773690637"
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
          "id": "638fcd649632fdc307597b02b079a89588c5d490",
          "message": "Fix thread-safety in SynchronizedList (#242)\n\n* fix(collections): resolve thread-safety issues in SynchronizedList\n\nReplaced the .NET 9 `System.Threading.Lock` with a standard `object` for `SyncRoot` to ensure consistent behavior between the `lock` statement and `Monitor` calls.\n\nRefactored `SynchronizedEnumerator<T>` from a `struct` to a `sealed class` with a `_disposed` flag. This ensures the lock is released exactly once and prevents potential issues with struct copying during enumeration.\n\nVerified the fix with reproduction tests confirming that concurrent modifications are now correctly blocked while an enumerator is held.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* fix(collections): ensure lock release in SynchronizedEnumerator.Dispose\n\nWrapped the underlying enumerator disposal in a try block and moved the Monitor.Exit call to a finally block to prevent lock leaks if disposal throws an exception.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-21T20:30:31+01:00",
          "tree_id": "548a38f4e73ead4b4a35f33d902335716cf56455",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/638fcd649632fdc307597b02b079a89588c5d490"
        },
        "date": 1774121859528,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.325257370869318,
            "unit": "ns",
            "range": "± 0.035764845143683215"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.163536809384823,
            "unit": "ns",
            "range": "± 0.004171874615208538"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 569.7702960968018,
            "unit": "ns",
            "range": "± 1.102381002809625"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.61292258898418,
            "unit": "ns",
            "range": "± 0.5886085274411267"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 998.9853763580322,
            "unit": "ns",
            "range": "± 14.724208794010012"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 100)",
            "value": 2697.5926780700684,
            "unit": "ns",
            "range": "± 8.339964572963883"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 100)",
            "value": 26029.691396077473,
            "unit": "ns",
            "range": "± 93.15191003482323"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 100)",
            "value": 814994.3722330729,
            "unit": "ns",
            "range": "± 6669.250828807551"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 100)",
            "value": 966933.4739583334,
            "unit": "ns",
            "range": "± 15051.386173572017"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 645.6288223266602,
            "unit": "ns",
            "range": "± 2.0714395995435653"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 100)",
            "value": 59.95182474454244,
            "unit": "ns",
            "range": "± 0.7267485914395486"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5062.623733520508,
            "unit": "ns",
            "range": "± 20.721282700222115"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4182.102355957031,
            "unit": "ns",
            "range": "± 16.008331984951987"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2250.935432434082,
            "unit": "ns",
            "range": "± 17.395156559751186"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4540.677538553874,
            "unit": "ns",
            "range": "± 41.75494622469292"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1290.4844004313152,
            "unit": "ns",
            "range": "± 12.447374640420872"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 149.1788868109385,
            "unit": "ns",
            "range": "± 2.7269578741210343"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2135.031220753988,
            "unit": "ns",
            "range": "± 0.4047244588262965"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1006.9187196095785,
            "unit": "ns",
            "range": "± 1.2692877241750555"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1091.348575592041,
            "unit": "ns",
            "range": "± 12.506318517562287"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 722.7321767807007,
            "unit": "ns",
            "range": "± 1.8908725617753188"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3768.30525970459,
            "unit": "ns",
            "range": "± 20.488732176027238"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0,
            "unit": "ns",
            "range": "± 0"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 58.87071712811788,
            "unit": "ns",
            "range": "± 0.31449122358988635"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.2154501006007195,
            "unit": "ns",
            "range": "± 0.00768502437600882"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5240.422019958496,
            "unit": "ns",
            "range": "± 10.967374402026858"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 631.2037159601847,
            "unit": "ns",
            "range": "± 1.9177081413022021"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9660.44748433431,
            "unit": "ns",
            "range": "± 19.21791897046727"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 1000)",
            "value": 2722.5321629842124,
            "unit": "ns",
            "range": "± 3.783911439702384"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 1000)",
            "value": 25994.929494222004,
            "unit": "ns",
            "range": "± 58.995092259587985"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 1000)",
            "value": 885427.9143880209,
            "unit": "ns",
            "range": "± 34285.19196395254"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 1000)",
            "value": 985637.5481770834,
            "unit": "ns",
            "range": "± 36735.26355147563"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 644.7486276626587,
            "unit": "ns",
            "range": "± 4.068488163935461"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 1000)",
            "value": 232.3879852294922,
            "unit": "ns",
            "range": "± 8.474588622809105"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 50232.356313069664,
            "unit": "ns",
            "range": "± 156.1864163357659"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 42261.320780436195,
            "unit": "ns",
            "range": "± 137.74540696182802"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22690.43487040202,
            "unit": "ns",
            "range": "± 54.589897124518004"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 48490.16864013672,
            "unit": "ns",
            "range": "± 65.7796470284816"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 14965.844355265299,
            "unit": "ns",
            "range": "± 50.51076199674673"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1528.010383605957,
            "unit": "ns",
            "range": "± 9.83764155303869"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10065.860717773438,
            "unit": "ns",
            "range": "± 24.191260049908248"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 996.8921203613281,
            "unit": "ns",
            "range": "± 1.0044033921552682"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9420.759785970053,
            "unit": "ns",
            "range": "± 37.619565861944956"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5312.23343149821,
            "unit": "ns",
            "range": "± 31.01118495798837"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 31568.151397705078,
            "unit": "ns",
            "range": "± 374.4424627133662"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.1022557703157266,
            "unit": "ns",
            "range": "± 0.1202362553557898"
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
          "id": "3e0738d55ebd3c15008eedd0a86980df181ad0ab",
          "message": "Fix Angular build ambiguity in multi-project workspace (#252)\n\n* fix(web-app): specify project name in Angular CLI scripts\n\nIn a multi-project workspace where projects share a common root or overlap, the Angular CLI cannot automatically determine which project to target for commands like build, serve, and test.\n\nThis commit updates `Hagalaz.Web.App/package.json` to explicitly pass `Hagalaz.Web.App` to all `ng` scripts, resolving the ambiguity and fixing the build failure.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* fix(web-app): specify project name in Angular CLI scripts\n\nIn Angular 21, the `defaultProject` property in `angular.json` is deprecated and causes schema validation errors. For multi-project workspaces where projects share a common root, the Angular CLI requires explicit project targeting.\n\nThis commit updates `Hagalaz.Web.App/package.json` to explicitly pass `Hagalaz.Web.App` to all `ng` scripts, ensuring deterministic builds in CI (GitHub Actions) while maintaining compatibility with the current Angular version. Both Web App and Admin builds are verified to pass.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-28T15:19:22+01:00",
          "tree_id": "1c9dd9cc6ac185180057483e2112efc6cb61969d",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/3e0738d55ebd3c15008eedd0a86980df181ad0ab"
        },
        "date": 1774707994832,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.29422347744306,
            "unit": "ns",
            "range": "± 0.020705684709790177"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1645362252990403,
            "unit": "ns",
            "range": "± 0.006863342455679237"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 573.291661898295,
            "unit": "ns",
            "range": "± 1.158233095531226"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.81845662991206,
            "unit": "ns",
            "range": "± 0.533446685551449"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 1015.0530325571696,
            "unit": "ns",
            "range": "± 4.511912315731656"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 100)",
            "value": 1818.957327524821,
            "unit": "ns",
            "range": "± 5.283717508487116"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 100)",
            "value": 18224.736231486004,
            "unit": "ns",
            "range": "± 84.34482156945714"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 100)",
            "value": 576043.6318359375,
            "unit": "ns",
            "range": "± 1235.190863275198"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 100)",
            "value": 686440.8557942709,
            "unit": "ns",
            "range": "± 9830.376982566035"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 650.0116608937582,
            "unit": "ns",
            "range": "± 3.4262465439488827"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 100)",
            "value": 60.470140874385834,
            "unit": "ns",
            "range": "± 0.3131713290627123"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5003.355735778809,
            "unit": "ns",
            "range": "± 25.744134708628984"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4200.901491800944,
            "unit": "ns",
            "range": "± 22.885515283562647"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2286.482275644938,
            "unit": "ns",
            "range": "± 12.394503682207297"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4594.971371968587,
            "unit": "ns",
            "range": "± 47.60480653144375"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1290.9092642466228,
            "unit": "ns",
            "range": "± 1.116905368275974"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 150.50329653422037,
            "unit": "ns",
            "range": "± 1.6153002500003424"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2142.2976252237954,
            "unit": "ns",
            "range": "± 2.07762606810227"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1007.7955989837646,
            "unit": "ns",
            "range": "± 1.2009878591916523"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1386.0024668375652,
            "unit": "ns",
            "range": "± 11.73122492139032"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 629.2491397857666,
            "unit": "ns",
            "range": "± 22.60979302342666"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3815.3046379089355,
            "unit": "ns",
            "range": "± 31.400438597575356"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.053739155953129135,
            "unit": "ns",
            "range": "± 0.046682131470036276"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 59.06314094861349,
            "unit": "ns",
            "range": "± 0.42519077686608964"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.18998846411705,
            "unit": "ns",
            "range": "± 0.009537972346318545"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5855.089922587077,
            "unit": "ns",
            "range": "± 15.411174869111962"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 630.7808542251587,
            "unit": "ns",
            "range": "± 1.219809280471259"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9927.988479614258,
            "unit": "ns",
            "range": "± 36.83759378914629"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 1000)",
            "value": 1833.972318013509,
            "unit": "ns",
            "range": "± 18.50337171333911"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 1000)",
            "value": 18264.793782552082,
            "unit": "ns",
            "range": "± 90.35614349933947"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 1000)",
            "value": 591600.5764973959,
            "unit": "ns",
            "range": "± 22132.84164481819"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 1000)",
            "value": 704218.01953125,
            "unit": "ns",
            "range": "± 13294.804824119994"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 647.4040533701578,
            "unit": "ns",
            "range": "± 2.979253181571986"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 1000)",
            "value": 240.57917340596518,
            "unit": "ns",
            "range": "± 16.759600562122692"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 51412.754374186195,
            "unit": "ns",
            "range": "± 106.26347032162786"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 42027.05039469401,
            "unit": "ns",
            "range": "± 187.0025615752595"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22490.96743265788,
            "unit": "ns",
            "range": "± 229.44884584456764"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 48167.09655761719,
            "unit": "ns",
            "range": "± 93.72579334493444"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 15016.032302856445,
            "unit": "ns",
            "range": "± 15.444116704701178"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1692.7136484781902,
            "unit": "ns",
            "range": "± 15.030734648173912"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 9947.284388224283,
            "unit": "ns",
            "range": "± 853.6374684244678"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 999.6250133514404,
            "unit": "ns",
            "range": "± 1.3157593595986217"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 12549.832087198893,
            "unit": "ns",
            "range": "± 205.3988746318246"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5311.776840209961,
            "unit": "ns",
            "range": "± 64.0770423801603"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 33713.197692871094,
            "unit": "ns",
            "range": "± 613.3384488474503"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.0013918826977411907,
            "unit": "ns",
            "range": "± 0.0020250573788070138"
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
          "id": "7b84bbbddb6f7adada68d0e91db6308d3c3de85e",
          "message": "⚡ Bolt: Optimize Enumerable.IndexOf by avoiding materialization (#250)\n\n* ⚡ Bolt: Optimize Enumerable.IndexOf by avoiding materialization\n\nOptimized the `IndexOf` extension method in `Hagalaz.Collections.Extensions` to iterate directly over the `IEnumerable` source. This eliminates the O(N) heap allocations caused by the previous `ToArray()` call and allows for early return upon finding a match.\n\nPerformance Impact:\n- N=100: ~51% speedup (342.7 ns -> 167.9 ns), 424 B -> 0 B allocations.\n- N=1000: ~25% speedup (2,065.1 ns -> 1,548.8 ns), 4,024 B -> 0 B allocations.\n\nVerification:\n- Existing tests in `Hagalaz.Collections.Extensions.Tests` passed.\n- New benchmark added to `Hagalaz.Benchmarks` for ongoing monitoring.\n- Correctness verified across dependent projects by running relevant tests.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Update .jules/bolt.md\n\nCo-authored-by: gemini-code-assist[bot] <176961590+gemini-code-assist[bot]@users.noreply.github.com>\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\n\n* ⚡ Bolt: Optimize Enumerable.IndexOf with IList fast path and no materialization\n\nOptimized the `IndexOf` extension method in `Hagalaz.Collections.Extensions` by:\n1. Adding a fast path for `IList<T>` (lists, arrays) using a simple `for` loop to avoid enumerator allocations and leverage $O(1)$ indexed access.\n2. Implementing a general path for other `IEnumerable<T>` sources using a `foreach` loop to avoid the previous `ToArray()` materialization and support early exit.\n\nPerformance Impact (on List<int>):\n- N=100: Speedup from 342.7 ns to ~208 ns, 424 B -> 0 B allocations.\n- N=1000: Speedup from 2,065.1 ns to ~2,278 ns (noise, but 4,024 B -> 0 B allocations).\n\nVerification:\n- All solution tests passed.\n- Benchmark `EnumerableIndexOf` added to `Hagalaz.Benchmarks`.\n- Documentation updated in `.jules/bolt.md`.\n\nCo-authored-by: frankvdb7 <5363672+frankvdb7@users.noreply.github.com>\n\n* Update .jules/bolt.md\n\nCo-authored-by: gemini-code-assist[bot] <176961590+gemini-code-assist[bot]@users.noreply.github.com>\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\n\n---------\n\nSigned-off-by: Frank <5363672+frankvdb7@users.noreply.github.com>\nCo-authored-by: google-labs-jules[bot] <161369871+google-labs-jules[bot]@users.noreply.github.com>\nCo-authored-by: gemini-code-assist[bot] <176961590+gemini-code-assist[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-28T15:19:43+01:00",
          "tree_id": "a6584636ec7ca84e47afea78e7a5abd78915665f",
          "url": "https://github.com/frankvdb7/Hagalaz/commit/7b84bbbddb6f7adada68d0e91db6308d3c3de85e"
        },
        "date": 1774708039177,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 100)",
            "value": 8.328769947091738,
            "unit": "ns",
            "range": "± 0.05182927617313974"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 100)",
            "value": 3.1768470828731856,
            "unit": "ns",
            "range": "± 0.0056428401734787475"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 100)",
            "value": 570.7071838378906,
            "unit": "ns",
            "range": "± 1.1830092725974939"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 100)",
            "value": 68.60667129357655,
            "unit": "ns",
            "range": "± 0.14842137006551834"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 100)",
            "value": 1001.9954306284586,
            "unit": "ns",
            "range": "± 3.383703442265833"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EnumerableIndexOf(N: 100)",
            "value": 100.45079203446706,
            "unit": "ns",
            "range": "± 0.16641305574918377"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 100)",
            "value": 1842.2325890858967,
            "unit": "ns",
            "range": "± 5.231501867803477"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 100)",
            "value": 18318.565622965496,
            "unit": "ns",
            "range": "± 53.95313474870155"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 100)",
            "value": 586447.9222005209,
            "unit": "ns",
            "range": "± 6621.8810151545185"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 100)",
            "value": 729615.2236328125,
            "unit": "ns",
            "range": "± 2308.725556566292"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 100)",
            "value": 644.5365692774454,
            "unit": "ns",
            "range": "± 1.3216057990461805"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 100)",
            "value": 59.93033482631048,
            "unit": "ns",
            "range": "± 0.1175979027464925"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 100)",
            "value": 5007.498929341634,
            "unit": "ns",
            "range": "± 28.056179545290192"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 100)",
            "value": 4184.547180175781,
            "unit": "ns",
            "range": "± 7.613701231310299"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 100)",
            "value": 2207.4331703186035,
            "unit": "ns",
            "range": "± 12.641710392067205"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 100)",
            "value": 4534.718490600586,
            "unit": "ns",
            "range": "± 10.40272955694882"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 100)",
            "value": 1306.6701723734539,
            "unit": "ns",
            "range": "± 10.61272384422329"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 100)",
            "value": 145.5255797704061,
            "unit": "ns",
            "range": "± 2.3723972663138944"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 100)",
            "value": 2222.0647735595703,
            "unit": "ns",
            "range": "± 66.60467607088017"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 100)",
            "value": 1006.4876085917155,
            "unit": "ns",
            "range": "± 0.8768160557352201"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 100)",
            "value": 1124.1810061136882,
            "unit": "ns",
            "range": "± 12.69812483607986"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 100)",
            "value": 681.976422627767,
            "unit": "ns",
            "range": "± 21.227667895498985"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 100)",
            "value": 3702.8980140686035,
            "unit": "ns",
            "range": "± 21.849003711794353"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 100)",
            "value": 0.0005493238568305969,
            "unit": "ns",
            "range": "± 0.000496593005213967"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListContains(N: 1000)",
            "value": 60.664327800273895,
            "unit": "ns",
            "range": "± 3.800940511808267"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetContains(N: 1000)",
            "value": 3.1991059333086014,
            "unit": "ns",
            "range": "± 0.004039075020515854"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ConcurrentStoreIteration(N: 1000)",
            "value": 5862.219778696696,
            "unit": "ns",
            "range": "± 27.257007677613363"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ListHashSetIteration(N: 1000)",
            "value": 632.7677405675253,
            "unit": "ns",
            "range": "± 4.939805766587047"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ToListHashSet_Benchmark(N: 1000)",
            "value": 9781.734329223633,
            "unit": "ns",
            "range": "± 70.67422991408363"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EnumerableIndexOf(N: 1000)",
            "value": 935.1208283106486,
            "unit": "ns",
            "range": "± 8.836339269219124"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Small(N: 1000)",
            "value": 1807.7993564605713,
            "unit": "ns",
            "range": "± 5.583078108113034"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Read_Large(N: 1000)",
            "value": 18103.642130533855,
            "unit": "ns",
            "range": "± 53.408699717476715"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Small(N: 1000)",
            "value": 592570.7194010416,
            "unit": "ns",
            "range": "± 16967.337251127607"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.FileStore_Write_Large(N: 1000)",
            "value": 748502.8701171875,
            "unit": "ns",
            "range": "± 35304.27083939931"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ComputeHash_Benchmark(N: 1000)",
            "value": 748.2446098327637,
            "unit": "ns",
            "range": "± 2.3263265501357955"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.GetStringInBetween(N: 1000)",
            "value": 217.1453906695048,
            "unit": "ns",
            "range": "± 7.7676338839044625"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.SelectIntFromString(N: 1000)",
            "value": 50719.40763346354,
            "unit": "ns",
            "range": "± 187.64935466095156"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeBoolValues(N: 1000)",
            "value": 43419.684102376305,
            "unit": "ns",
            "range": "± 30.789232194172822"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_StringDelegate(N: 1000)",
            "value": 22926.601267496746,
            "unit": "ns",
            "range": "± 124.6336359888881"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.DecodeIntValues_SpanDelegate(N: 1000)",
            "value": 48346.8163655599,
            "unit": "ns",
            "range": "± 95.4439124170383"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeIntValues(N: 1000)",
            "value": 15274.840825398764,
            "unit": "ns",
            "range": "± 30.827212140112437"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.EncodeBoolValues(N: 1000)",
            "value": 1698.521666208903,
            "unit": "ns",
            "range": "± 11.827996598204841"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_Old_List(N: 1000)",
            "value": 10066.770543416342,
            "unit": "ns",
            "range": "± 18.58988128331837"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.Viewport_New_ListHashSet(N: 1000)",
            "value": 998.9973360697428,
            "unit": "ns",
            "range": "± 1.870899667714758"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Linq(N: 1000)",
            "value": 9404.7045211792,
            "unit": "ns",
            "range": "± 24.302208416193846"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportUpdateTick_Manual(N: 1000)",
            "value": 5301.203815460205,
            "unit": "ns",
            "range": "± 14.961714234804386"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_Old(N: 1000)",
            "value": 32429.622100830078,
            "unit": "ns",
            "range": "± 48.26938450629611"
          },
          {
            "name": "Hagalaz.Benchmarks.HagalazBenchmarks.ViewportTypedAccess_New(N: 1000)",
            "value": 0.0009558672706286112,
            "unit": "ns",
            "range": "± 0.00083161917395038"
          }
        ]
      }
    ]
  }
}