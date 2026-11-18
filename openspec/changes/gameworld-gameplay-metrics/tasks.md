# Tasks

## 1. Combat deaths

- [x] 1.1 Record deaths at the existing player and NPC combat state transition. Use the existing GameWorld meter and add focused measurement tests for both actor kinds, nonlethal damage, duplicate calls before respawn, delayed callbacks, and a new death after spawn. Assert one increment per transition and the full tag set.
- [x] 1.2 Count resolved kill credit once in the original death task. Verify bounded killer and victim tags, no credit before resolution or for unattributed deaths, and no duplicate from later callbacks.

## 2. Successful skill actions

- [x] 2.1 Record successful Cooking actions after inventory replacement in the scripts plugin. Drive the real task through controlled ticks and verify one increment per cooked item, no duplicate after further ticks on a finished batch, and no increment for interruption or missing input. Assert the full tag set.
- [x] 2.2 Count the first accepted Fishing catch per attempt using the existing skill counter. Verify one count for multiple catches and none for attempts with no inventory reward.

## 3. Integration checks

- [x] 3.1 Update the SigNoz guide with kill-credit and Fishing context and the five-minute rate panels.
- [x] 3.2 Verify all instruments use the subscribed GameWorld meter. Run the focused suites, solution build, strict OpenSpec validation, and diff check after the gameplay context changes.
