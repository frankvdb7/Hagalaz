# Design

## Current behavior

`Hagalaz.Services.Cache/Program.cs` configures OpenIddict validation to call Authorization's introspection endpoint as `cache-service-1`, using `Security:Introspection:ClientSecret`. Authorization startup currently provisions only `storm-app` and `storm-admin`. The captured Aspire trace for the admin archive-sizes request shows that the introspection POST receives HTTP 401 and Authorization logs `invalid_client` for `cache-service-1`; the Cache API request then returns HTTP 500.

## Chosen design

- Keep client registration owned by Authorization's existing startup registration service.
- Rename that service to `FirstPartyClientRegistration` because it will own both browser clients and the Cache API service client.
- Define a secret Aspire parameter in AppHost and pass it to Authorization and Cache API as `Security:Introspection:ClientSecret`. The existing AppHost `UserSecretsId` is the local source of the parameter; no service appsettings file contains the secret.
- Require a non-blank secret in Authorization before registration. If the setting is missing, fail startup with a clear configuration error.
- Create it as a confidential web application with that secret and only the `Endpoints.Introspection` permission. The application type is required by the existing database schema.
- If an existing cache client validates against the configured secret, leave it unchanged. If validation fails, populate its descriptor and update only its client secret so local secret rotation does not leave the database registration out of sync.
- Extend the current registration unit tests to assert all three client descriptors and preserve the existing web-client scope constraints.

Set the local AppHost parameter with `dotnet user-secrets set "Parameters:cache-introspection-client-secret" <value> --project Hagalaz.AppHost/Hagalaz.AppHost.csproj`.

## Alternatives considered

- Changing the admin token's scopes would not fix the traced 401: the introspection request is rejected because its client ID is unknown.
- Changing Cache API's 500 mapping would hide the failure while leaving token validation unavailable.
- Registering the API client in AppHost would move authorization database ownership out of the Authorization service.

## Risks

Authorization and Cache API must receive the same secret in each environment. Aspire passes one parameter to both resources; other hosting environments must supply matching `Security:Introspection:ClientSecret` values.

## Open questions

None.
