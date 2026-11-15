## MODIFIED Requirements

### Requirement: Ordinary underlays blend across tile boundaries

The map viewer MUST retain decoded floor tint when textures load and MUST interpolate ordinary opaque underlay colours and textures across shared tile boundaries using the available selected terrain. Opaque effect 7 underlays MUST blend the same way and keep their reflection. Texture scales and plane separation MUST remain intact. Transparent surfaces and surfaces with any other material effect MUST retain their dedicated material path.

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
