## Context

The game client reads interfaces from cache index 3. Each reference-table group is a widget, and its child files are widget components.

## Decisions

- Use the existing reference-table endpoint. Actual group and child IDs support sparse indexes without scanning unused IDs. Dedicated endpoints or a component decoder would add server work without improving the metadata shown here.
- Put the list and detail under a common Widgets route. A route-provided service owns loading and retry of the reference table. The router owns the page number and selected widget ID.
- Keep the detail page and its template independent of the list so later inspection tools can extend it.
- Read the selected widget through the existing archive API. Join archive slots to component IDs using the reference table. Decode the fields needed by the workspace and reuse authenticated sprite image loading. The detail page owns temporary layout and visibility changes.
- Load font metrics and glyph sprites with the existing container API and share the decoded font per preview. Text components draw the bitmap glyphs with their own layout and appearance settings.
- Render cached model components with the existing model image pipeline and their authored widget camera. The preview shares requests for repeated models with the same camera and size. The inspector links to the existing interactive Models view.
- Use one WebGL renderer per widget preview and share prepared geometry, textures, and animation frames. Batch visible requests by model, camera, size, and pose. Composite into the component layers to preserve widget draw order. Use the PNG pipeline when WebGL or the live asset is unavailable.
