# Design

## Context

Tab widgets share click delegates that carry component IDs, click types, and item or slot data. Individual handlers often consume only the fields relevant to their action.

## Goals / Non-Goals

- Remove at least 100 additional source warnings in tab widget callbacks.
- Preserve component IDs, delegate signatures, click filtering, and side effects.

## Decisions

- Refresh the warning inventory from the current working tree. Select unused lambda parameters in tab widget registrations.
- Use discard syntax for unused inputs. Preserve names read by the handler or captured by nested callbacks.
- Use source search for test discovery to avoid coverage workers. Add and run characterization cases through the registered click handlers before editing production source.
- Compare warning counts with matching Rider settings, rebuild the solution, and rerun the focused and full Game Scripts tests with bounded non-coverage execution.

## Risks / Trade-offs

- A parameter can be consumed inside a nested callback. Confirm the whole handler before replacing a name.
- Multiple unused parameters can share a discard only where the C# syntax supports it. Verify compilation and retain the delegate's original arity and types.
