## MODIFIED Requirements

### Requirement: Underwater terrain layer

The map preview SHALL draw a region's underwater terrain and objects beneath its surface when the region has underwater data, and SHALL let the user hide them.

#### Scenario: Region has underwater data

- **WHEN** the anchor or neighbouring selected region has a `um` archive
- **THEN** its floors render at the surface height plus the decoded depth, and its `ul` objects render on that layer

#### Scenario: Region has no underwater data

- **WHEN** the `um` archive is absent
- **THEN** the region renders as before

#### Scenario: Hide the layer

- **WHEN** the user clears the underwater terrain option
- **THEN** the layer's floors and objects are removed without reloading the region

#### Scenario: Hide the water surface

- **WHEN** the user clears the water surface option
- **THEN** floors whose material uses a water effect (2, 4, 5, 8 or 9) are omitted from the surface planes, revealing the layer beneath, while other floors remain

#### Scenario: Water above underwater terrain

- **GIVEN** a surface uses effect 8 or 9 and the region environment and water program are available
- **WHEN** the surface is drawn
- **THEN** the client shoreline calculation uses the depth beneath each surface vertex and blends the water over the underwater layer

### Requirement: Region sky and fog

The map preview SHALL use the selected anchor region's decoded sky colour as the viewport background while region atmosphere is active, and SHALL offer linear fog derived from the region's raw fog value.

#### Scenario: Region supplies sky and fog

- **WHEN** the selected anchor region contains a lighting record with sky colour or fog flags
- **THEN** the map render response includes the sky colour and raw fog value, and the viewer clears to that colour

#### Scenario: Lighting record omits sky and fog

- **WHEN** the lighting record does not set the sky colour or fog flags
- **THEN** the response carries the client defaults `0xC8C0A8` and `0`

#### Scenario: Preview fog

- **WHEN** the user enables fog while region atmosphere is active
- **THEN** geometry fades linearly to the sky colour between `14332 - ((fogValue + 256) << 2)` and `14332` scene units of eye depth

#### Scenario: Region supplies an environment map

- **WHEN** the selected anchor region's lighting record sets the environment-map flag and all six face textures load while region atmosphere is active
- **THEN** material effects 7, 8 and 9 sample a cube built from those faces in `+X, -X, +Y, -Y, +Z, -Z` order, effect 1 keeps its procedural sky, and the face images are not listed as surface textures unless a surface also uses them

#### Scenario: Environment map is unavailable

- **WHEN** the record omits the faces, a face fails to load, or default lighting is selected
- **THEN** effect 7 retains its existing base-material fallback and effects 8 and 9 use the client base-material path

#### Scenario: Default lighting

- **WHEN** region atmosphere is unavailable or the user selects default lighting
- **THEN** the viewer keeps its existing background and draws no fog

#### Scenario: Water program is unavailable

- **WHEN** the viewer cannot use the client water program
- **THEN** effects 8 and 9 use the base-material path

### Requirement: Ordinary underlays blend across tile boundaries

The map viewer MUST retain decoded floor tint when textures load and MUST interpolate ordinary opaque underlay colours and textures across shared tile boundaries using the available selected terrain. Opaque effect 7 underlays MUST blend the same way and keep their reflection. Texture scales and plane separation MUST remain intact. Water and other special-effect floors SHALL retain their dedicated material paths.

#### Scenario: Adjacent underlays have different materials
- **GIVEN** neighboring terrain tiles have different ordinary underlay textures or colours
- **WHEN** their shared edge is rendered
- **THEN** both tiles use matching normalized material contributions on that edge
- **AND** each texture retains its own world-space scale
- **AND** the transition does not depend on transparent coplanar overlay passes

#### Scenario: Texture loading finishes
- **WHEN** a terrain texture becomes available
- **THEN** the decoded grass or path tint remains applied instead of turning white

#### Scenario: A reflective underlay meets an ordinary underlay
- **GIVEN** neighboring terrain tiles use an opaque effect 7 underlay and an ordinary underlay
- **WHEN** their shared edge is rendered
- **THEN** the textures blend across the edge
- **AND** the region environment map is reflected in proportion to the effect 7 layer's weight and texture alpha

#### Scenario: A blend has no environment map
- **GIVEN** a terrain blend contains an effect 7 layer and the region has no environment map
- **WHEN** the blend is rendered
- **THEN** the layers blend without any reflection

## ADDED Requirements

### Requirement: Water material effects use the client programs

The map viewer SHALL render water material effects 2, 4, 5, 8 and 9 using the revision-742 client's noise, decoded parameters and render equations when the required inputs are available.

#### Scenario: Animated texture stages

- **WHEN** a map floor uses effect 2, 4 or 5
- **THEN** its texture coordinates and colour stages follow the client program and material flags

#### Scenario: HD water reflection

- **WHEN** a map floor uses effect 8 or 9 with the required water resources and region environment
- **THEN** the water uses the client normal field, cube reflection, sunlight, shoreline alpha and fog
