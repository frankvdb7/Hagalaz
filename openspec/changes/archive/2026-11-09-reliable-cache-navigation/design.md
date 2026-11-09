# Design

The activated route owns applied browsing parameters. Components own draft query text and existing form/file models. Existing NgRx stores retain request ownership. Route changes dispatch once; explicit resubmission of an unchanged query remains an intentional refresh.

One cache-scoped pending-edits service reads the active editor's values and mutation state; it does not copy form data. Child activation protects reused ID routes, while shell deactivation protects navigation outside Cache Studio. Both call the same confirmation policy. Native beforeunload uses that same active-editor predicate and removes its listener with the scope.

Compare editable form values with the authoritative loaded definition so successful store updates establish the new baseline, while failed writes retain differences. Sprite file selection remains its existing component-owned signal and clears only upon confirmed upload.

Rejected: CanDeactivate only (misses reused detail IDs), global navigation history/draft stores (second owner), per-link confirmation (misses Back/sidebar), asynchronous unload dialogs (browser cannot await them).

Logout requests guarded navigation to the exact login route before invoking the existing auth owner. This preserves authentication when editing continues. Existing provider-failure cleanup remains unchanged. Superseding navigation attempts share the existing Material discard dialog.

Numeric query deep links replace their history entry and drop the numeric query from the destination. Interactive ID lookups retain normal history. This prevents Back replay of legacy numeric URLs.
