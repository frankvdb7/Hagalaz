# cache-item-images Specification

## Purpose

Defines how the cache service serves item sprite images and how Cache Studio displays them in item results.

## Requirements

### Requirement: Authenticated item sprite retrieval

The Cache service MUST return the supplied GIF image for an item through the existing authenticated cache API.

#### Scenario: An item sprite exists

- **WHEN** an authorized caller requests the image for an item with a supplied sprite
- **THEN** the service returns HTTP 200, the exact GIF bytes, and the `image/gif` content type

#### Scenario: An item sprite is missing

- **WHEN** an authorized caller requests the image for an item without a supplied sprite
- **THEN** the service returns HTTP 404 without returning an image body

### Requirement: Cache Studio shows item sprites

Cache Studio MUST show an item's sprite alongside that item in the item results table using the authenticated cache API.

#### Scenario: An item sprite loads

- **WHEN** an item result has a supplied sprite
- **THEN** Cache Studio displays that sprite in the item's row

#### Scenario: An item sprite cannot be loaded

- **WHEN** an item result's sprite request fails
- **THEN** Cache Studio shows an unavailable preview while keeping the item row and its definition link usable
