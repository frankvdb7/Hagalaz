# Design

## Context

The existing CS2 detail response contains decoded operands and display-oriented source rows. Widget Play posts events and a snapshot to a Cache service interpreter. The page already owns the renderer and the Play/Stop lifecycle.

## Goals / Non-Goals

Keep the Cache service authoritative for decoding and make the browser authoritative for preview execution and temporary state. See the proposal for exclusions.

## Decisions

- **Define a versioned RuneScript subset with explicit stack semantics.** Emit typed literals, locals, calls, labels, branches, switches, and named game operations from the decoded script. Preserve instruction indices, exact 64-bit operands, and explicit unsupported operations. Use a structured syntax tree as the transport and render the corresponding readable RuneScript in the source view. This keeps the rendered source and the program tied to the same operations without parsing display text or evaluating generated JavaScript.
- **Load a widget program before Play.** Use the cached component callback bindings to find script roots. Resolve transitive numeric callees once per load and include each script once, including recursive references. Keep missing callees identifiable so unrelated callbacks can still run. Reuse the guarded Cache API and cancel the load on Stop or navigation.
- **Interpret the program in TypeScript.** Give each call its own typed locals and share the operand stacks within an event. Use Java-compatible integer arithmetic and BigInt for signed 64-bit values. Port verified effects from the current server preview to a small widget API that can update the temporary canvas and simulated variables. Apply instruction and call-depth limits. Direct JavaScript evaluation would grant capabilities outside the widget runtime.
- **Commit an event atomically.** The page owns program loading, retry, and the preview instance. The runtime runs each event against a copy of the current preview state and returns changes only on success. Preserve open order, pointer coordinates, trigger filters and reset behavior. Remove the server interpreter and its route after wiring the browser runtime.

## Risks / Trade-offs

- The current high-level pseudocode can omit stack operations. Executable output must preserve them and show unsupported operations at their original indices.
- A load can complete after Stop or navigation. Bind each load to the current preview instance and discard late results.
- The preview has a subset of the client state. Report unsupported operations with the script ID and instruction, leaving the prior snapshot intact.

## Migration Plan

Deploy the program-loading API before switching the admin. Remove the execution endpoint with the client migration. Reconcile the active `widget-script-preview` requirement with browser execution before archive.
