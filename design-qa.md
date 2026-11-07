# Region editor design QA

- Reference image: C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-8f80941c-f2de-4bb0-aaa1-439ca0334aff.png
- Implementation image: C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-33c44850-8d9d-4e25-9d72-41bc6a9d5e12.png
- Route and state: /cache/types/maps/12850?kind=maps, dark theme, Plane 0 and landscape visible, keys and warnings collapsed.
- Normalization: the 1654 × 951 reference and 2309 × 1322 implementation capture have nearly the same aspect ratio. Comparison uses proportions within each viewport because CSS dimensions and device pixel ratios are unknown.

The capture shows the intended two-column workspace, selection row, toolbar, and collapsed panels. The map occupies about 51% of the viewport width, versus about 93% in the reference. It also sits lower and is viewed from a shallower, more oblique angle. The form fields sit above the button centers, and the shell background shows through the inspector.

The follow-up frames the map using its projected bounds and a higher camera angle, aligns the form fields, and gives the inspector its own surface. A projection test confirms that a flat region fits tightly without clipping. The axis gizmo and lighting selector are still absent, and the grid remains available through the View menu.

The second implementation capture (C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-29d97e1f-b249-432f-b107-332fc1009b69.png, 2309 × 1342) confirms aligned controls and a solid inspector. The map still spans about half the viewport width, and the river runs front to back. The camera now faces across the river at a shallower angle. A projection test at the captured viewport aspect ratio checks horizontal coverage and clipping. A post-change capture is pending.


The map now draws the Three.js orientation gizmo in the upper-right of the canvas. The renderer tests verify overlay order and disposal. Browser appearance is pending a new capture.

The supplied gizmo close-ups show that circular markers and unlabeled negative ends make the axes harder to read. The revised overlay uses three colored arrows, a gray center cube, and standalone X, Y, and Z labels. The revised appearance awaits a browser capture.

## Viewport-first reference

Source image: C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-1766fcc5-af5c-4892-98b9-e0a02a7f71c8.png (1672 × 941).

The implementation places region selection, settings and loading beside camera and display controls. The viewport fills the available height and starts with the grid visible. The inspector remains beside it on desktop and stacks below it on narrow screens. The Region editor omits the region-key input, scene warnings panel, and standalone Back to Maps button.

The admin production build and eight focused tests passed. A screenshot of the updated layout at the reference viewport and state is pending. Browser capture remains user-operated.

## Object editor reference

Source image: C:/Users/fmavd/AppData/Local/Temp/codex-clipboard-16c2a39a-74bc-41d3-a6b6-00691777eda8.png (1671 × 941).

The updated layout has the live map, object tool strip, selection badge, tabbed inspector, and loaded asset tray. The tray uses asset type icons and real IDs. Transform edits use inspector fields. The object bounding box remains the visual selection marker. Cache persistence, definition-level property and appearance editing, and draggable transform handles are outside the implemented preview.

A browser capture of the updated workspace at the reference size with an object selected is pending. Browser capture remains user-operated.

final result: blocked
