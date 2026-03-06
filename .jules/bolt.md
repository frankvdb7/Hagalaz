## 2026-02-20 - [Optimizing Viewport Lookups]
**Learning:** Transitioning a collection from `List` to `HashSet` to improve `Contains()` speed ((n) \to O(1)$) can introduce a regression in index-based access ((1) \to O(n)$ via `ElementAt`). In game engines where both lookups and random access/iteration are common, a hybrid collection like `ListHashSet` is necessary to maintain performance across all use cases.
**Action:** Always check for indexer usage (`[]`) before changing `IReadOnlyList` to `IReadOnlyCollection` or `IEnumerable`. Use a hybrid collection when both access patterns are performance-critical.

## 2026-03-01 - Optimized String Encoding with string.Create
**Learning:** Using `StringBuilder` for small/simple joins can be less efficient than `string.Join` or `string.Create`. `string.Create` is particularly powerful for specialized encoding (like boolean to "1/0") as it allows zero-allocation of intermediate buffers and exact sizing of the final string.
**Action:** Prefer `string.Create` for high-performance string building when the final length is known and the encoding logic is simple.

## 2026-03-03 - [Allocation-Free Iteration with Struct Enumerators]
**Learning:** Returning `IEnumerator<T>` from a collection's `GetEnumerator()` method always causes a 56B heap allocation due to boxing the enumerator struct into an interface. In high-frequency game logic (e.g., viewport updates), this adds significant GC pressure. Additionally, accessing `ConcurrentDictionary.Values` in some .NET versions triggers a full snapshot of the collection into a new array, leading to O(N) allocations.
**Action:** Always return the concrete struct enumerator (e.g., `List<T>.Enumerator`) from `GetEnumerator()` to enable allocation-free `foreach` loops. For `ConcurrentDictionary`, implement manual `Any` and `FirstOrDefault` methods that iterate over the dictionary directly to avoid snapshotting.

## 2026-03-06 - [Optimizing Network Packet Serialization and String Handling]
**Learning:** Standard `MemoryStream` and `StringBuilder` usage in high-frequency hot paths (like networking) introduces significant GC pressure and redundant memory copies. `MemoryStream.ToArray()` and `SerializeBuffer()` both create new arrays, leading to O(3N) allocations during serialization. Furthermore, reading strings byte-by-byte from a stream using `StringBuilder` is significantly slower than direct buffer access via `TryGetBuffer` and `Encoding.GetString`.
**Action:** Use `Buffer.BlockCopy` into a single destination array for serialization. Always initialize `MemoryStream` with `publiclyVisible: true` to enable zero-copy buffer access for parsing. Prefer `string.Create` for complex diagnostic string building to eliminate intermediate `StringBuilder` and `Append` overhead.
