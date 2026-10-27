# Proposal

## Why

Cache Studio item rows need the item sprite dump, but serving those files from the Angular admin assets produced 404s in the already-running Aspire web resource. The cache service already owns cache-type APIs, so it should own and serve these images through its existing authenticated API boundary.

## What Changes

- Move the item GIF dump into `Hagalaz.Services.Cache/wwwroot/images/items`.
- Add an authenticated item-image endpoint that returns the matching GIF or 404 when it is absent.
- Have the admin request item images through its existing authenticated `HttpClient` path and render the returned blobs in the item sprite column.
- Remove the duplicated admin static asset copy.

## Capabilities

### New Capabilities

- `cache-item-images`: Authenticated retrieval and Cache Studio display of item sprite images.

### Modified Capabilities

None.

## Impact

- Cache API route under `/api/v1/cache/types/items/{id}/image`, service web-root assets, and Cache service tests.
- Admin `CacheItemsService`, an item thumbnail component, and Cache Studio table tests.

## Non-goals and stop conditions

- Do not change cache API authorization, generate or transform images, change the normal player app, or add a dependency.
- Stop if the route cannot remain inside the existing cache authorization group or if the supplied GIF dump cannot be served from the service web root without changing the cache API security model.

## Acceptance criteria

- An authorized request for an existing item image returns its exact GIF bytes with `image/gif`; a missing image returns 404.
- Cache Studio item rows retrieve images through `CacheItemsService` and display them without direct browser requests to static frontend assets.
- Existing sprite previews and other definition rows retain their current behavior.
- Focused Cache service and admin tests pass, and the Cache service build includes the moved GIF files.

## Existing mechanisms reused

- The cache API's existing `/api/v1/cache` authorization group.
- `CacheItemsService` and the registered Angular auth interceptor for bearer-authenticated requests.
- The existing blob-thumbnail pattern for displaying and releasing object URLs.
