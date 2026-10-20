## 1. Authorization behavior

- [x] 1.1 Remove the persisted-valid-token online-session check from credential sign-in while preserving the existing password-grant and token-creation response; verify with `SignInUserRequestConsumerTests.Consume_WhenValidPersistedTokenExists_IssuesFreshTokensInsteadOfReturningAlreadyAuthenticated`.

## 2. Regression coverage and validation

- [x] 2.1 Add authorization consumer tests proving valid credentials issue fresh tokens without querying persisted tokens when an old valid token exists, while invalid credentials still stop before token creation.
- [x] 2.2 Verify the 21 `AuthenticationSignInTests` cases covering active lobby/world duplicate-session rejection, run the 21 authorization tests, and pass strict OpenSpec validation.
