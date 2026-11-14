# Widget workspace design QA

final result: passed

Compared the supplied 1672 × 941 mockup with the supplied signed-in Widget 762 capture at 2312 × 1348. The side-by-side comparison normalized the images to the same displayed width. The different viewport sizes and widget content limit pixel-level comparison.

- The first live capture showed a preview occupying about 44% of the stage width, a workspace extending below the viewport, and a clipped Widgets tab. The revised capture shows the centered Bank preview occupying about 76% of the stage width in Fit mode, all three panel bottoms in view, and Widgets visible and selected.
- The revised capture shows the hierarchy and component search on the left, preview tools above the checkerboard, and Properties and Logic on the right. Labels, fields, outlines, and selection colors follow the existing admin theme. The earlier capture also showed the inspector populated for sprite component 36.
- The mockup shows a populated Bank with a different widget ID. The live preview renders the cached Bank definition and sprite images. The inventory contents in the mockup depend on game state. Preview text uses the admin font.
- Focused Angular tests covered widget navigation, sparse component IDs, selection, zoom, local layout edits, and reset. The admin production build passed.
