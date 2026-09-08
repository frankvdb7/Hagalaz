## 1. Region decode contract

- [x] 1.1 Add a complete-region map-provider operation that reports full local coordinates and all effective planes, verified by focused provider tests
- [x] 1.2 Keep the existing chunk-oriented decode behavior unchanged, verified by the existing `DecodePart_ValidData_InvokesCallbacks` test

## 2. GameWorld integration

- [x] 2.1 Load static cache objects through the complete-region operation and preserve their collision application, verified by the region-loader build and provider regression tests

## 3. Verification

- [x] 3.1 Run the focused cache tests, strict OpenSpec validation, and `git diff --check`
