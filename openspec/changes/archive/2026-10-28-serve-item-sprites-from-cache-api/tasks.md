# Tasks

## 1. Cache service image endpoint

- [x] 1.1 Move the supplied GIF dump into `Hagalaz.Services.Cache/wwwroot/images/items` and add `GET /types/items/{id:int}/image` inside the existing cache API group; verify endpoint tests return the exact GIF bytes and `image/gif` for an existing file and 404 for a missing file.
- [x] 1.2 Extend the cache test host to use a temporary web root and add endpoint tests for existing and missing item sprites; verify the focused Cache service test project passes.

## 2. Admin item thumbnails

- [x] 2.1 Add an authenticated blob request to `CacheItemsService` and an item thumbnail component that displays the blob, shows a fallback on failure, and releases its object URL; verify focused component tests cover request, fallback, and URL cleanup.
- [x] 2.2 Render the item thumbnail component in the item preview column and remove the admin static asset configuration/copy; verify Cache Studio page tests cover the item column while sprite preview tests still pass.

## 3. Integration verification

- [x] 3.1 Build and publish the Cache service, build the admin app, run focused backend and admin tests, and validate the OpenSpec change; verify published output contains the item GIF dump and all checks pass.
