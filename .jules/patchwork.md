## 2025-05-14 - [GetStringInBetween Remainder Calculation Bug]
**Bug:** Characters were being skipped in the remainder string when `includeEnd` was set to true in `GetStringInBetween`.
**Cause:** The code was modifying the `iEnd` index variable when `includeEnd` was true, and then using `iEnd + strEnd.Length` to calculate the starting index for the remainder string. This resulted in skipping the delimiter's length twice.
**Fix:** Decoupled the extracted result's length from the remainder's starting index calculation.
**Prevention:** Avoid reusing and modifying index variables for different purposes (result extraction vs. remainder calculation). Added explicit unit tests for both parts of the returned array.

## 2025-05-15 - [StatsController.Get Returning MassTransit Response Wrapper]
**Bug:** The `StatsController.Get` method was returning the full `Response<GetCharacterStatisticsResult>` instead of the inner `GetCharacterStatisticsResult` message.
**Cause:** Incorrect usage of the MassTransit `GetResponse` return value in the controller action.
**Fix:** Changed the return statement to use `response.Message` instead of `response`.
**Prevention:** Always extract the `Message` property from MassTransit/Mediator responses before returning them in a Web API controller. Added unit tests for `StatsController` to verify correct return types.
