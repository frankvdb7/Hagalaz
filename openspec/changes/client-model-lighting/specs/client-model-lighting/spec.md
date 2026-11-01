# Spec Delta

## Purpose

Display isolated cache models using the GameClient's model-lighting semantics under a deterministic inspection light.

## ADDED Requirements

### Requirement: Preserve model lighting inputs
The model preview contract SHALL preserve face shading modes and effective model ambient and contrast for supported raw and definition-backed models. Multipart assembly SHALL retain source face identity and shading modes.

#### Scenario: Definition lighting
- **GIVEN** item, NPC and object definitions with non-default lighting modifiers
- **WHEN** their models are requested
- **THEN** effective ambient and contrast match the corresponding client construction path, applying each decoded scale exactly once

#### Scenario: Shading mode decode
- **GIVEN** supported legacy and newer models containing smooth, flat and suppressed faces
- **WHEN** decoded and assembled
- **THEN** each face retains its shading mode and associations

### Requirement: Apply client model shading once
The preview SHALL apply client-compatible smooth and flat normals, directional response, packed-colour lighting and textured intensity under a fixed client-derived inspection light. Prepared colours SHALL NOT receive a second generic lighting pass. Explicitly unlit material effects SHALL remain unlit.

#### Scenario: Smooth and flat surfaces
- **GIVEN** adjoining smooth and flat triangles with known client lighting results
- **WHEN** previewed under the same inspection light
- **THEN** corner colours match the characterized client equations and integer rounding

#### Scenario: Directional and brightness boundaries
- **GIVEN** positive and negative light dot products and brightness values at clamp boundaries
- **WHEN** lighting textured and untextured faces
- **THEN** output matches the corresponding client response and clamp rules

#### Scenario: Alpha sentinels and unlit effects
- **GIVEN** hidden or constant-shading alpha sentinel faces and an unlit material
- **WHEN** preparing their appearance
- **THEN** visibility and constant shading follow client semantics
- **AND** directional lighting does not darken the explicitly unlit material

### Requirement: Lighting tracks posed geometry
Animation SHALL update lighting for the current pose while preserving transparency priorities, UVs, material identity and particle face references. The preview SHALL disclose that isolated inspection lighting does not reproduce map atmosphere or all client backends.

#### Scenario: Animated model
- **GIVEN** a posed model with transparent faces and attached particles
- **WHEN** animation changes its geometry
- **THEN** shading follows the current pose without changing source face associations or transparency priorities

#### Scenario: Inspection limits
- **WHEN** inspecting a supported model
- **THEN** preview notes distinguish supported model lighting from unverified scene lighting and backend parity
