## 2026-02-20 - [Optimizing Viewport Lookups]
**Learning:** Transitioning a collection from `List` to `HashSet` to improve `Contains()` speed ((n) \to O(1)$) can introduce a regression in index-based access ((1) \to O(n)$ via `ElementAt`). In game engines where both lookups and random access/iteration are common, a hybrid collection like `ListHashSet` is necessary to maintain performance across all use cases.
**Action:** Always check for indexer usage (`[]`) before changing `IReadOnlyList` to `IReadOnlyCollection` or `IEnumerable`. Use a hybrid collection when both access patterns are performance-critical.

## 2026-03-01 - Optimized String Encoding with string.Create
**Learning:** Using `StringBuilder` for small/simple joins can be less efficient than `string.Join` or `string.Create`. `string.Create` is particularly powerful for specialized encoding (like boolean to "1/0") as it allows zero-allocation of intermediate buffers and exact sizing of the final string.
**Action:** Prefer `string.Create` for high-performance string building when the final length is known and the encoding logic is simple.
