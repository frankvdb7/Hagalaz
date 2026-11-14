## Context

OrbitControls scales both gestures by the camera's distance to its focus. A perspective camera sees different depths across the map, so the fitted side nearest the camera responds more strongly.

## Decisions

Use the midpoint of the visible terrain's vertical bounds as a stable reference plane. A ray-plane intersection gives the scale without traversing scene objects. Reject intersections outside the camera's clip range or at grazing distances.

Keep OrbitControls as camera owner. For a valid right drag, capture that pointer and scale the existing screen pan by the grabbed point's view depth. For a wheel gesture, use the pointer-ray distance as the focus radius before OrbitControls applies its wheel step. Leave other gestures with OrbitControls and release listeners and pointer capture with the map scene.
