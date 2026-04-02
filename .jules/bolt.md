## 2026-02-20 - [Optimizing Viewport Lookups]
**Learning:** Transitioning a collection from `List` to `HashSet` to improve `Contains()` speed ((n) \to O(1)$) can introduce a regression in index-based access ((1) \to O(n)$ via `ElementAt`). In game engines where both lookups and random access/iteration are common, a hybrid collection like `ListHashSet` is necessary to maintain performance across all use cases.
**Action:** Always check for indexer usage (`[]`) before changing `IReadOnlyList` to `IReadOnlyCollection` or `IEnumerable`. Use a hybrid collection when both access patterns are performance-critical.

## 2026-03-01 - Optimized String Encoding with string.Create
**Learning:** Using `StringBuilder` for small/simple joins can be less efficient than `string.Join` or `string.Create`. `string.Create` is particularly powerful for specialized encoding (like boolean to "1/0") as it allows zero-allocation of intermediate buffers and exact sizing of the final string.
**Action:** Prefer `string.Create` for high-performance string building when the final length is known and the encoding logic is simple.

## 2026-03-03 - [Allocation-Free Iteration with Struct Enumerators]
**Learning:** Returning `IEnumerator<T>` from a collection's `GetEnumerator()` method always causes a 56B heap allocation due to boxing the enumerator struct into an interface. In high-frequency game logic (e.g., viewport updates), this adds significant GC pressure. Additionally, accessing `ConcurrentDictionary.Values` in some .NET versions triggers a full snapshot of the collection into a new array, leading to O(N) allocations.
**Action:** Always return the concrete struct enumerator (e.g., `List<T>.Enumerator`) from `GetEnumerator()` to enable allocation-free `foreach` loops. For `ConcurrentDictionary`, implement manual `Any` and `FirstOrDefault` methods that iterate over the dictionary directly to avoid snapshotting.

## 2026-03-13 - [Reducing Allocations with ReadOnlySpan in String Slicing]
**Learning:** Using `Substring` repeatedly to slice strings during parsing creates multiple transient heap-allocated string objects, which significantly increases GC pressure. Replacing these with `ReadOnlySpan<char>` allows for O(1) slicing without any allocations. In `GetStringInBetween`, this reduced heap allocations by ~49% and improved execution time by ~42%.
**Action:** Always prefer `ReadOnlySpan<char>` and `AsSpan()` when performing complex string parsing or multi-step slicing. Only call `ToString()` at the final step when a persistent string object is actually required.

## 2026-03-27 - [Optimizing Enumerable.IndexOf by Avoiding Materialization]
**Learning:** Using `ToArray()` on an `IEnumerable<T>` to perform a search via index is highly inefficient as it materializes the entire collection into memory (O(N) space) and prevents early return. Iterating directly with `foreach` and a manual counter achieves O(1) space and allows immediate exit upon finding the match. In `CollectionExtensions.IndexOf`, this reduced execution time by ~51% (N=100) and eliminated all managed allocations (424B -> 0B).
**Action:** Avoid `ToArray()`, `ToList()`, or `ElementAt()` when a simple pass over an `IEnumerable` is sufficient. Always prefer single-pass, allocation-free iteration for search operations.

## 2026-04-02 - [Optimizing HashSet.AddRange by Avoiding Aggregate]
**Learning:** Using LINQ's `Aggregate` to implement `HashSet.AddRange` is highly inefficient due to delegate overhead and repeated virtual calls. Replacing it with a simple `foreach` loop and using `EnsureCapacity` for collections with a known size significantly reduces execution time (~49%) and heap allocations (~70% for N=1000).
**Action:** Always prefer `foreach` loops over `Aggregate` or complex LINQ chains in high-performance collection utilities. Use `EnsureCapacity` when the number of items to be added is known or can be estimated.

## 2026-04-02 - [Fast Paths for IReadOnlyList in Enumerable Extensions]
**Learning:** Adding fast paths for `IList<T>`, `T[]`, and `List<T>` in `IEnumerable` extension methods (like `IndexOf`) is common, but also including a fast path for `IReadOnlyList<T>` captures custom high-performance collections like `ListHashSet<T>`. This avoids the 16-32B allocation for an enumerator and the overhead of interface-based enumeration.
**Action:** When writing extension methods for `IEnumerable<T>`, include fast paths for `List<T>`, arrays, and `IReadOnlyList<T>` to maximize performance across both standard and custom collections.
