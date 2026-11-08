# cache-model-images Specification

## Purpose
Define the rendered PNG previews the Cache API provides for models.

## Requirements

### Requirement: Model image preview

The cache service SHALL render a cached model to a transparent PNG, centred and fitted to the requested size, and SHALL support conditional revalidation.

#### Scenario: Render a model

- **WHEN** a client requests an existing model's image with a width and height from 8 to 512
- **THEN** the service returns a PNG of that size with the model centred and filling most of the image

#### Scenario: Render an object

- **WHEN** a client requests an object's image, optionally for one shape
- **THEN** the service returns a PNG of that shape's models with the definition's scale and recolours, or 404 if the object has no such shape

#### Scenario: Map editor catalog

- **WHEN** the map editor's object catalog shows a placeable object in tile view
- **THEN** the tile displays the object's rendered preview once it nears the viewport, and otherwise keeps its placeholder icon

#### Scenario: Unchanged model

- **WHEN** the request's validator matches the current image
- **THEN** the service returns 304 without rendering

#### Scenario: Invalid or missing model

- **WHEN** the size is out of range or the model does not exist
- **THEN** the service returns 400 or 404 respectively
