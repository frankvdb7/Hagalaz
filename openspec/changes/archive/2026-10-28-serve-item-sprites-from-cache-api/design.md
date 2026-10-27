# Design

## Context

`MapCacheEndpoints` applies the cache authorization policy to every cache route. `TypeEndpoints` owns item and sprite routes. The admin already sends cache requests through `CacheItemsService`, and its auth interceptor adds the bearer token. See the proposal and `specs/cache-item-images/spec.md` for motivation and behavior.

## Goals / Non-Goals

**Goals:** Keep item images beside the cache API, serve original GIF bytes behind existing authorization, and let Cache Studio display them using authenticated requests.

**Non-Goals:** Change cache authorization, decode or transform item images, add cache storage or retry state, or add images to the normal player app.

## Decisions

1. **Serve files from the Cache service web root.** Store the dump at `wwwroot/images/items/{id}.gif`. Map `GET /types/items/{id:int}/image` inside `TypeEndpoints`; resolve the numeric filename with `IWebHostEnvironment.WebRootFileProvider`, return `image/gif` bytes, and return 404 when no file exists. The route stays inside the existing authorized cache group. A public static-file mapping was rejected because it would bypass the API authorization boundary.
2. **Fetch item images through the existing Angular HTTP path.** Add a blob request to `CacheItemsService`, allowing the registered auth interceptor to send the bearer token. A direct `<img src>` to the protected API cannot attach that token, and making image access anonymous would widen the existing security boundary.
3. **Keep item preview state with its row thumbnail.** Add a focused item thumbnail component that owns its HTTP subscription, temporary object URL, and error placeholder, and revokes the object URL when its input changes or the component is destroyed. Keep the existing sprite thumbnail and sprite store unchanged; that component owns frame selection and sprite preview state.
4. **Move the copied dump out of the Angular app.** Keep the supplied files in Cache service `wwwroot` so the Web SDK includes them with that service. Remove the admin-only asset configuration and duplicate asset copy.

## Risks / Trade-offs

- The service package gains about 24 MB of image content. This is the supplied authoritative dump and avoids requiring the admin server to have a separate local data path.
- A missing item GIF is reported as 404; the thumbnail displays an unavailable marker and leaves the definition link usable.

## Migration Plan

Move the existing copied GIF directory from the admin assets to the Cache service web root, add the API and admin client flow, then restart only the Cache service resource under Aspire to load the new web-root content. Rollback removes the route and thumbnail and moves the directory back; no cache database or archive data is changed.
