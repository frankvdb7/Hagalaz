## 2025-05-15 - Incorrect remainder calculation in GetStringInBetween
**Bug:** When extracting a string between two delimiters using `GetStringInBetween`, the remainder of the string (the part after the closing delimiter) was incorrectly truncated if `includeEnd` was set to `true`.
**Cause:** The method was adding the length of the end delimiter to the already-incremented `endIndex` when calculating the start of the remainder substring, causing it to skip characters.
**Fix:** Refactored the method to calculate `result0Length` and `remainderStartIndex` independently based on the initial delimiter index, ensuring that `remainderStartIndex` is always `endIndex + strEnd.Length` relative to the current search string.
**Prevention:** Avoid mutating index variables in-place when they are needed for multiple subsequent calculations. Use descriptive, immutable variable names for different stages of the string manipulation.
