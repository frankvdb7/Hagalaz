# cache-item-images Specification

## Purpose

Defines how the cache service serves item sprite images and how Cache Studio displays them in item results.

## Requirements

### Requirement: Authenticated item sprite retrieval

The Cache service MUST render an item's inventory icon from the current cache contents and return it as a PNG through the existing authenticated cache API. Without options, the icon MUST match the client's inventory icon: 36×32 pixels, the client camera and lighting for the item's 2D icon settings, outline level 1 and the inventory drop shadow.

#### Scenario: An item sprite exists

- **WHEN** an authorized caller requests the image for an item that has an inventory model
- **THEN** the service returns HTTP 200 with the `image/png` content type and a 36×32 image
- **AND** the image applies the item's recolours, retextures, model scale, zoom, rotations and offsets
- **AND** visible model pixels have a one-pixel outline and the inventory drop shadow, and all other pixels are fully transparent

#### Scenario: A noted, lent or bought item is rendered

- **WHEN** the item references a note, lend or bought template
- **THEN** the image composites the referenced item on its template in the same order as the client

#### Scenario: Rendering is deterministic

- **WHEN** the same item is requested twice with the same options and unchanged cache contents
- **THEN** both responses contain identical image bytes and the same `ETag`

#### Scenario: An unchanged item sprite is revalidated

- **WHEN** an authorized caller requests an item image with an `If-None-Match` value that matches the current `ETag`
- **THEN** the service returns HTTP 304 without an image body
- **AND** the response includes the `ETag` and `Cache-Control: private, no-cache`

#### Scenario: The item or its model changes

- **WHEN** the item definition, its inventory model or a texture it uses changes in the cache
- **THEN** the next request renders the new appearance and returns a different `ETag`

#### Scenario: A duplicated item is requested

- **WHEN** an item was just created by duplicating another item
- **THEN** its image request returns a rendered icon without any additional setup

#### Scenario: An item sprite is missing

- **WHEN** an authorized caller requests the image for an item without an inventory model, or whose model is missing from the cache
- **THEN** the service returns HTTP 404 without an image body

#### Scenario: An item model uses an unsupported format

- **WHEN** the item's model cannot be decoded for rendering
- **THEN** the service returns HTTP 422 with a problem description and no image body

### Requirement: Cache Studio shows item sprites

Cache Studio MUST show an item's sprite alongside that item in the item results table using the authenticated cache API.

#### Scenario: An item sprite loads

- **WHEN** an item result has a supplied sprite
- **THEN** Cache Studio displays that sprite in the item's row

#### Scenario: An item sprite cannot be loaded

- **WHEN** an item result's sprite request fails
- **THEN** Cache Studio shows an unavailable preview while keeping the item row and its definition link usable

### Requirement: Customisable item icon output

The item image endpoint MUST accept optional parameters that change the icon's size, outline and shadow. Omitted parameters MUST keep the client defaults.

#### Scenario: A larger icon is requested

- **WHEN** a caller requests an item image with `width` and `height` within the supported range
- **THEN** the PNG has exactly the requested dimensions
- **AND** the item keeps the client framing, scaled uniformly to fit the requested size

#### Scenario: The outline is changed

- **WHEN** a caller requests `outline=0`, `outline=1` or `outline=2`
- **THEN** the icon has no outline, the client's dark outline, or the dark outline followed by the client's light outer outline respectively

#### Scenario: The shadow is changed or removed

- **WHEN** a caller requests `shadow` with a six-digit RGB colour, or `shadow=none`
- **THEN** the drop shadow uses that colour, or is omitted

#### Scenario: Options produce distinct cache validators

- **WHEN** the same item is requested with different options
- **THEN** each response has its own `ETag`

#### Scenario: Invalid options are rejected

- **WHEN** `width` or `height` is outside the supported range, `outline` is not 0, 1 or 2, or `shadow` is neither `none` nor a six-digit RGB colour
- **THEN** the service returns HTTP 400 with a validation message and no image body

### Requirement: Item icon quantity

The item image endpoint MUST accept an optional `quantity` and `quantityMode` and render the client's stack model and quantity label for them. Without a `quantity` the icon MUST use quantity 1. Without a `quantityMode`, the label MUST be drawn only for stackable items with a quantity above zero.

#### Scenario: A quantity reaches a stack amount

- **WHEN** a caller requests an item image with a quantity at or above one of the item's stack amounts
- **THEN** the icon renders the stack variant for the highest amount the quantity reaches

#### Scenario: The default label only marks stackable items

- **WHEN** a caller requests an item image without `quantityMode`
- **THEN** stackable items with a quantity above zero show their quantity, including one, and ordinary items or quantities of zero or less show no label

#### Scenario: The quantity label follows the mode

- **WHEN** `quantityMode` is 0, 1 or 2, matching the client modes set by interface scripts
- **THEN** mode 0 draws no label, mode 1 always draws one, and mode 2 draws one only for stackable items or quantities other than one, never for -1

#### Scenario: Large quantities are abbreviated

- **WHEN** a label is drawn
- **THEN** quantities below 100,000 are yellow, quantities below 10,000,000 are white thousands with `K`, and larger quantities are green millions with `M`, each with a one-pixel dark shadow

#### Scenario: The label font is missing

- **WHEN** a label must be drawn and the cache has no `p11_full` font
- **THEN** the service returns HTTP 422 with a problem description and no image body

#### Scenario: Invalid quantity options are rejected

- **WHEN** `quantity` is below -1 or `quantityMode` is not 0, 1 or 2
- **THEN** the service returns HTTP 400 with a validation message and no image body
