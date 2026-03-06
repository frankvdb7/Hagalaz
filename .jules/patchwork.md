## 2025-05-14 - [GetStringInBetween Remainder Calculation Bug]
**Bug:** Characters were being skipped in the remainder string when `includeEnd` was set to true in `GetStringInBetween`.
**Cause:** The code was modifying the `iEnd` index variable when `includeEnd` was true, and then using `iEnd + strEnd.Length` to calculate the starting index for the remainder string. This resulted in skipping the delimiter's length twice.
**Fix:** Decoupled the extracted result's length from the remainder's starting index calculation.
**Prevention:** Avoid reusing and modifying index variables for different purposes (result extraction vs. remainder calculation). Added explicit unit tests for both parts of the returned array.

## 2025-05-15 - [SynchronizedList Synchronization Mismatch]
**Bug:** `SynchronizedList<T>` enumerator did not correctly lock the collection against modifications from other threads.
**Cause:** The code was using the .NET 9 `System.Threading.Lock` type for `SyncRoot` but using `Monitor.Enter/Exit` (via the `SynchronizedEnumerator` constructor/Dispose) for enumeration locking. These use different underlying mechanisms, allowing other threads to acquire the lock via `lock(SyncRoot)` while the enumerator thought it held it.
**Fix:** Reverted `SyncRoot` to a standard `object` to ensure consistent `Monitor`-based locking across all operations. Also changed `SynchronizedEnumerator` from a `struct` to a `class` to safely manage lock state and disposal.
**Prevention:** Avoid mixing new .NET 9 `Lock` types with legacy `Monitor` methods or `lock` statements if the lock must be held across method boundaries. Ensure the same lock object and mechanism are used throughout the class.
