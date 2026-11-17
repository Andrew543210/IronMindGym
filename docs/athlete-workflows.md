# Training workflows

## In the gym

Search by exercise name or browse a muscle group. A new exercise starts with zero external load; values from an unrelated exercise are never carried over. For bodyweight work, zero means no added weight. Keep the same recording convention between sessions: dumbbells per hand, barbell including the bar, unilateral repetitions per side. Logged load × reps is a tracking measure, not a complete estimate of work or a comparison between different exercises.

The previous saved set is shown for reference. **Use these values** copies it only on request. After logging a set, the same exercise retains its values for the next set. **Edit** changes an existing set without restarting rest or creating another set. Warm-up, working and failure labels remain visible on mobile and in workout details.

Session duration follows elapsed clock time, including time in a background tab. Enter a manual duration in Session settings to override it. Rest is a user-selected timer, not a prescribed recovery interval or measured actual rest. The saved rest value is the configured value.

The per-tab draft contains completed sets, session start time, timer deadline, training day, duration override and review. An empty abandoned draft starts a new clock. Successful saving clears the draft. Closing the tab or clearing browser data can lose it; server saving requires a connection.

## Comparisons and persistence

- Alternating A → B → A remains in that order after saving. Summary statistics group the same exercise once, while the set list preserves the sequence.
- A load record requires a positive external load in a strength exercise. It is the heaviest logged load, not an estimated 1RM. Bodyweight repetitions and timed exercise loads do not become strength load records.
- Historical summaries compare against earlier sessions, including a deterministic ID tie-breaker for equal timestamps.
- Deleting the last weighted session removes its record if only zero-load sets remain. Earned awards remain historical.
- Progress shows separate changes in load, repetitions, duration and perceived effort. Higher subjective effort is not a percentage improvement in fitness.
- Baseline and current progress values are submitted together and committed in one transaction. Failed validation preserves the previous pair. Fields stay locked until saved values finish loading; stale responses cannot overwrite another selection.
- Nutrition uses personal targets where configured. Missing macro inputs do not manufacture a carbohydrate target. Unconfigured targets are labelled **Not set**, and disabled estimates are labelled **Off**.

The catalog data migration updates descriptions and the mobility classification without replacing exercise IDs. It also removes invalid zero-load records left by older deletion behavior. Back up the database before an application upgrade as described in the deployment guide.

## Verification

The regression suite covers circuit order, ownership of previous-set references, bodyweight and timed records, historical comparisons, deletion recalculation, atomic progress rollback, partial nutrition targets and catalog migration with existing workout rows. The full suite contains 50 passing checks at this revision.

Browser checks at a 390 × 844 viewport covered searching, logging, editing, alternating strength/cardio sets, saving, opening the result, using the previous-set reference, starting a clean session, and saving a duration/effort comparison. The workout form had no horizontal page overflow, and no browser console errors were observed. Slow/stale progress responses and retry behavior were additionally exercised with a focused JavaScript harness.

The UI remains English and calendar boundaries use UTC. This check does not establish medical suitability, native mobile background alerts or offline support.
