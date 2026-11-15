## Production reachability and ownership

The legacy route is the only production entry to SpritesManagerPageComponent. Current sprite detail pages use SpriteInspectorComponent instead. Only the legacy manager calls CacheSpritesStore.loadSprite/searchSprites, which exclusively call CacheSpritesService.getSprite/searchSprites. Current metadata browsing uses CacheSpriteDefinitionsStore and definition/range/search resources. Remove the obsolete branch; preserve previewSpriteImage, uploadSpriteImage and clearError used by Sprite Studio.

## Navigation

Reuse the existing current browser `/cache/types?kind=sprites` and detail route `/cache/types/sprites/:id`. Derive selected resource from router state, including query parameters. Use the existing router guards without a second selected-tab state or compatibility route. Old `/cache/sprites` URLs follow the existing unknown-route behavior.

## Invariants

Current sprite editing, request cancellation, image cleanup and staged-file guards remain unchanged. Removed legacy tests must not remove current editor coverage; retain the staged PNG guard case for SpriteInspectorComponent. Remove tests only for APIs actually retired, and adapt shared error-clear coverage to a current image preview.
