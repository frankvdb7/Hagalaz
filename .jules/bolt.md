## 2025-05-14 - Refactoring Obsolete Dialogues
**Learning:** Refactoring legacy specialized dialogue classes (`CookingDialogue`, `FletchingDialogue`, etc.) into a generic `InteractiveDialogueScript` reduces code duplication and improves maintainability by centralizing click handling and interface state management.
**Action:** Always look for patterns where multiple specialized scripts perform similar UI tasks and consider consolidating them into a generic, parameterizable script like `InteractiveDialogueScript`.
