## Decisions

- Use Angular Material for the workspace: tree, action list, tabs, menus, button toggles, checkboxes and tooltips.
- Group the tree by client object shape (walls, wall decoration, objects, roofs, ground decoration), because the cache has no building or prop categories.
- Build the tree from source placements rather than drawn ones, so hidden groups keep their rows.
- Show only properties the object definition or tile flags prove, as read-only values. Definition editing belongs to Cache Studio, so the inspector links there.
