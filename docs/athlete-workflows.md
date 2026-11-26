# Training workflows

## In the gym

Search by exercise name or browse a muscle group. A new exercise starts with zero external load; values from an unrelated exercise are never carried over. For bodyweight work, zero means no added weight. Keep the same recording convention between sessions: dumbbells per hand, barbell including the bar, unilateral repetitions per side. Logged load × reps is a tracking measure, not a complete estimate of work or a comparison between different exercises.

The last saved set of the exercise is shown for reference. **Use these values** copies it only on request. After logging a set, the same exercise keeps its values for the next set. **Edit** changes an existing set without restarting rest or creating another set. Warm-up, working and failure labels stay visible on mobile and in workout details.

Session duration follows elapsed clock time, including time in a background tab. Enter a manual duration in **Session settings** to override it. Rest is a timer you choose, not a prescribed recovery interval or measured actual rest; the saved rest value is the configured one.

## The unfinished workout

The draft holds the logged sets, session start, rest timer deadline, split and training day, duration override and review.

- It is kept in `localStorage` per athlete, so it survives a reload, a closed tab or a tab the phone discarded. Open tabs share it.
- Once it has a set, it is also sent to the server 1.5 s after the last change. Opening the logger on another device continues it there. An empty logger never replaces a workout started elsewhere.
- When two copies differ, a draft with sets wins over an empty one; otherwise the later change wins.
- An empty draft older than two hours starts a new clock. A start time in the future (a wrong device clock) is ignored.
- Saving the workout deletes the draft everywhere. Repeating the save, for example after a lost response, returns the workout saved the first time. A device still holding that draft is told the workout was saved elsewhere.

## Comparisons and records

- Alternating A → B → A stays in that order after saving. Summary statistics group the same exercise once, while the set list keeps the sequence, and set numbering continues when an exercise returns.
- A load record needs a positive external load in a strength exercise. It is the heaviest logged load, not an estimated 1RM. Bodyweight repetitions and loads of timed exercises do not become strength records.
- Workout details compare each exercise with sessions logged before it, with the id as a tie-breaker for equal times. Later workouts never change that comparison.
- Deleting a workout recalculates the affected records; a record disappears when only zero-load sets remain. Earned awards stay earned.
- Progress shows separate changes in load, repetitions, duration and perceived effort. Higher subjective effort is not presented as a percentage improvement in fitness.
- The starting and current results of a tracked exercise are saved together in one transaction. If either is invalid, neither changes. A late answer for a previously selected exercise cannot overwrite the current one.

## Days and weeks

"Today", the training week (Monday to Sunday) and streaks follow the time zone in the profile. The browser suggests it at registration, and **Edit profile** can change it, for example to `Europe/Kyiv`. An empty value means UTC. A workout logged at 23:30 in Kyiv belongs to that Kyiv day even though it is stored in UTC.

## Bodyweight

Setup records the first weigh-in for today. Weigh-ins can be added for any day in the last five years, one per day, and the latest one is the current bodyweight. Changing the weight in **Edit profile** logs it for today; changing only the height or the targets adds no weigh-in.

## Nutrition

One entry per calendar day; saving a day again replaces it, so a missed day can be filled in later. Days in the last five years up to today are accepted. Personal targets are used where configured. Without them, adults who opted into the energy equation get estimates: Mifflin–St Jeor BMR times an activity factor from the weekly plan, 1.6 g/kg protein, 0.8 g/kg fat and carbohydrates from the remaining energy. Missing inputs never produce a target; such targets are labelled **Not set**. A target counts as met within 10%.
