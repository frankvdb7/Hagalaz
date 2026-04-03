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

## 2025-05-16 - [StatsController.GetAll NullReferenceException on Deconstruction]
**Bug:** The `StatsController.GetAll` method would crash with a `NullReferenceException` if the request body was empty.
**Cause:** Attempting to deconstruct a null `GetAllCharacterStatisticsRequest` object (`var (sort, filter) = request;`).
**Fix:** Added an explicit null check for the `request` parameter and return `BadRequest()` if it is null.
**Prevention:** Always perform null checks on `[FromBody]` parameters in ASP.NET Core controllers before deconstructing or accessing their properties. Added a unit test to verify that a null request returns `BadRequest`.
