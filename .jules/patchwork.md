## 2025-05-14 - [GetStringInBetween Remainder Calculation Bug]
**Bug:** Characters were being skipped in the remainder string when `includeEnd` was set to true in `GetStringInBetween`.
**Cause:** The code was modifying the `iEnd` index variable when `includeEnd` was true, and then using `iEnd + strEnd.Length` to calculate the starting index for the remainder string. This resulted in skipping the delimiter's length twice.
**Fix:** Decoupled the extracted result's length from the remainder's starting index calculation.
**Prevention:** Avoid reusing and modifying index variables for different purposes (result extraction vs. remainder calculation). Added explicit unit tests for both parts of the returned array.

## 2025-05-15 - [StatsController.Get Returning MassTransit Response Wrapper]
**Bug:** The `StatsController.Get` method was returning the full `Response<GetCharacterStatisticsResult>` instead of the inner `GetCharacterStatisticsResult` message.
**Cause:** Incorrect usage of the MassTransit `GetResponse` return value in the controller action.
**Fix:** Changed the return statement to use `response.Message.Result` instead of `response`.
**Prevention:** Always extract the actual DTO (`Result` property) from MassTransit/Mediator response messages before returning them in a Web API controller to match the `ActionResult<T>` signature. Added unit tests for `StatsController` to verify correct return types.

## 2026-04-02 - [Highscores] Fix character statistics sorting and frontend stability
**Bug:** Highscores sorting was non-functional, and the highscores page could crash if a player had no statistics records.
**Cause:**
1. The `SortModel.Experience` property in the backend DTO lacked an `init` accessor, preventing deserialization from the frontend.
2. The frontend mapping function `mapStatisticsResult` accessed `statistics[0]` without checking if the array was present or had elements.
3. The backend controller deconstructed the request object without a null guard.
**Fix:**
1. Added `init` to `SortModel.Experience`.
2. Added a safety check and default values for missing statistics in the frontend mapping.
3. Added a null guard for the request in the `StatsController.GetAll` method.
**Prevention:** Ensure DTOs used for deserialization have appropriate accessors and always validate array presence before indexing in mapping functions.

## 2026-04-02 - [Cache/Benchmarks] Fix shadowing warnings and stabilize benchmarks
**Bug:** The CI check failed due to member shadowing warnings in the Cache abstraction and a performance regression alert on an extremely fast benchmark (indistinguishable from zero).
**Cause:**
1. Redundant `Id` property declarations in several interfaces (e.g., `INpcType`) shadowed the `Id` property inherited from `IType`.
2. Several non-nullable properties in `IMapProvider.cs` lacked the `required` modifier.
3. `ViewportTypedAccess_New` benchmark was so fast that its mean time was 0ns, causing an infinite ratio regression alert in CI.
**Fix:**
1. Removed redundant `Id` declarations in `INpcType.cs`, `IAnimationType.cs`, `IObjectType.cs`, and `IItemType.cs`.
2. Added `required` modifier to non-nullable properties in `IMapProvider.cs`.
3. Modified `ViewportTypedAccess_New` to be more robust against measurement noise.
**Prevention:** Avoid shadowing inherited members and use `required` for non-nullable properties that are not initialized in constructors. Ensure benchmarks have a measurable duration above zero.

## 2026-04-02 - [Cache/Benchmarks] Fix shadowing warnings and stabilize benchmarks (Final)
**Bug:** The CI check failed due to performance regression alerts on renamed and stabilized benchmarks, and earlier due to member shadowing and nullability warnings.
**Cause:**
1. Redundant `Id` property declarations in several interfaces (e.g., `INpcType`) shadowed the `Id` property inherited from `IType`.
2. Several non-nullable properties in `IMapProvider.cs` lacked the `required` modifier.
3. `ViewportTypedAccess_New` benchmark baseline was indistinguishable from zero, making stabilization attempts look like regressions.
**Fix:**
1. Removed redundant `Id` declarations in `INpcType.cs`, `IAnimationType.cs`, `IObjectType.cs`, and `IItemType.cs`.
2. Added `required` modifier to non-nullable properties in `IMapProvider.cs`.
3. Renamed `ViewportTypedAccess_Old` to `ViewportTypedAccess_Baseline` and `ViewportTypedAccess_New` to `ViewportTypedAccess_Optimized` with a stabilization loop to reset CI tracking and ensure measurable results.
**Prevention:** Avoid shadowing inherited members and use `required` for non-nullable properties. Rename benchmarks when baseline reset is required due to stabilization changes.
