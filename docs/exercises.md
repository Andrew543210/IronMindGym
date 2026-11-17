# Exercise catalog

The canonical catalog is `IronMindGym/Seed/exercises.json`: 123 uniquely named exercises, eight navigation groups and three tracking modes.

| Mode | Saved values | Examples |
| --- | --- | --- |
| StrengthTraining | External load, repetitions, rest | Squat, press, curl, push-up |
| Cardio | Duration, intensity 1–10, rest | Bike, rowing intervals, jump rope |
| Timed | Duration, optional external load, rest | Plank, dead hang, mobility holds, loaded carries |

For bodyweight exercises, load means additional external weight; use 0 when no additional load is used. Timed entries do not create maximum-load strength records.

## Classification

Groups are navigation aids based on a practical primary emphasis. Compound movements train several muscles; placement in one group does not mean isolation.

- Romanian deadlifts and good mornings belong with hamstrings; conventional deadlifts with the posterior chain.
- Weighted planks belong with core anti-extension, and side planks with lateral stability.
- Standing dumbbell presses belong with front delts; lateral raises and upright rows with side delts.
- Face pulls appear once under rear delts; a pause variation remains a separate option.
- Light and heavy versions of a good morning are one exercise with different logged loads.
- A barbell glute bridge is distinct from a hip thrust with an elevated upper back.
- Jump rope belongs with conditioning rather than calf isolation.
- Mobility has its own group. It is not treated as cardio.
- Core subgroups describe movement functions rather than implying separate upper/lower abdominal muscles.

The catalog was checked against the movement and body-part descriptions in the [ACE exercise library](https://www.acefitness.org/resources/everyone/exercise-library/) and the exercise patterns in [NSCA Foundations of Fitness Programming](https://www.nsca.com/contentassets/8323553f698a466a98220b21d9eb9a65/foundationsoffitnessprogramming_201508.pdf). Descriptions are short original navigation notes, not copied exercise instructions.

To change the catalog after people have saved workouts, add a migration or an explicit catalog update. Startup does not delete and recreate exercise IDs, because saved sets reference them. The `ClarifyExerciseCatalog` data migration updates descriptions in place and moves the general full-body mobility entry to timed Mobility while preserving its ID and saved durations. Existing effort values are retained as historical data, but timed entries do not use them.

## Recording and comparisons

Workout sets retain their entered order, including alternating exercises in supersets and circuits. Set numbering continues for an exercise when it returns later in the same session. Summaries combine its sets once per workout.

The maximum-load record is the highest positive external load logged for a strength exercise, not an estimated one-repetition maximum. A zero-load bodyweight set does not create this record. Timed exercises do not use strength record percentages. Progress comparisons display the changes in both recorded values: load and repetitions, duration and perceived effort, or duration and load. Duration multiplied by perceived effort is not presented as a fitness improvement score.

Catalog descriptions use movement descriptions rather than claims about guaranteed safety or isolated upper/lower abdominal muscles. The [ACE crunch entry](https://www.acefitness.org/resources/everyone/exercise-library/52/crunch/) is an additional reference for the trunk-flexion description. Broad legacy entries remain available so historical logs keep their meaning; their notes ask users to identify the exact drill for comparisons.
