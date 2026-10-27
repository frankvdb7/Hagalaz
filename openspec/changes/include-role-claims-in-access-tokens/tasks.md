# Tasks

## 1. Add scoped role destinations

- [x] 1.1 Add a focused helper that assigns only the access-token destination to role claims when `roles` is granted; add tests for granted and omitted `roles` scope cases.
- [x] 1.2 Apply the helper to password and refresh-token grants using each grant's effective scopes; verify both token-generation paths call the helper.

## 2. Verify administrator API access

- [x] 2.1 Run the focused Authorization and Cache policy tests; verify the Cache API policy still requires both `SystemAdministrator` and `cache_api`.
- [x] 2.2 Restart Authorization and Cache through Aspire, wait for both resources to become healthy, and run strict OpenSpec validation.
- [ ] 2.3 With a newly issued admin access token, confirm its introspection response includes `SystemAdministrator` and the archive-sizes endpoint returns 200.
