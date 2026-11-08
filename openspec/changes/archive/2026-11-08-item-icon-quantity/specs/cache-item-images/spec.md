# Spec Delta

## ADDED Requirements

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
