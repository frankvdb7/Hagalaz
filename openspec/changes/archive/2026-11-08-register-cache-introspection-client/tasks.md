# Tasks

## 1. Register the Cache API client

- [x] 1.1 Extend Authorization's existing startup registration to create `cache-service-1` as a confidential client with only introspection endpoint permission; verify `RegisterIfMissingAsync_WhenAllClientsAreMissing_CreatesClientsWithSeparatePermissions` preserves the existing web-client scope separation.
- [x] 1.2 Update an existing cache-client registration when its secret differs from configuration; `RegisterIfMissingAsync_WhenCacheClientSecretChanged_UpdatesSecretAndPreservesPermissions` verifies the new secret is stored and endpoint permissions are preserved.

## 2. Store the shared secret in User Secrets

- [x] 2.1 Define one secret Aspire parameter, pass it to Authorization and Cache API, and remove the checked-in placeholders; verify no appsettings file contains the introspection secret.
- [x] 2.2 Set a generated local value using `dotnet user-secrets` for the AppHost; verify the same value reaches both service configurations without printing it.

## 3. Verify the failure path

- [x] 3.1 Add and run registration regression tests for initial client creation and configured-secret rotation, including client ID, application type, confidential type, secret, and introspection permission; the focused registration suite passes.
- [x] 3.2 Restart the AppHost after its model change, wait until Authorization, Cache API, and admin are healthy, then verify an invalid-token archive-sizes request returns 401 while its introspection call returns 200; run strict OpenSpec validation.
