## Decisions

- Raw models have no client framing, so the preview uses a fixed three-quarter camera and adjusts zoom until the model fills 92% of the image on its limiting axis. This is a preview, not a client-authored image.
- Model positions are centred and the rendered bounds are shifted to the image centre, because the icon camera's integer offsets are too coarse and off-origin models would otherwise clip.
- Framing counts only vertices of drawn faces, so hidden faces and unused vertices cannot shrink the model.
- Reuse the icon rasteriser, lighting, texture loading and validator. Outlines, shadows and quantity labels are icon-only.
- Object previews use the definition's scale, recolours and retextures; the object definitions index joins the validator's sources. Placement offsets are dropped because the preview is centred.
- Catalog tiles fetch lazily near the viewport and keep the placeholder when an object has no shape or cannot be rendered.
- Projection uses 64-bit intermediates so very large models cannot overflow.
