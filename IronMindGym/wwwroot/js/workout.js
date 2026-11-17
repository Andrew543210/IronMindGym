"use strict";
(() => {
    const form = document.getElementById("finishWorkoutForm");
    if (!form) return;
    const catalog = JSON.parse(
        document.getElementById("exerciseCatalog").textContent,
    );
    const get = (id) => document.getElementById(id);
    const group = get("muscleGroupFilter"),
        subgroup = get("muscleSubGroupFilter"),
        exercise = get("exerciseSelect");
    const storageKey = `ironmind-workout-${form.dataset.userId}`;
    let draft = [],
        timerEnd = 0,
        saved = false;
    const payload = get("draftPayloadJson");
    try {
        const serverDraft = JSON.parse(payload.value || "[]");
        const stored = JSON.parse(sessionStorage.getItem(storageKey) || "null");
        draft = serverDraft.length ? serverDraft : stored?.sets || [];
        timerEnd = stored?.timerEnd || 0;
    } catch {
        draft = [];
    }

    function persist() {
        payload.value = JSON.stringify(draft);
        try {
            sessionStorage.setItem(
                storageKey,
                JSON.stringify({ sets: draft, timerEnd }),
            );
        } catch {
            /* Logging still works if browser storage is unavailable. */
        }
    }
    function options(select, rows, placeholder) {
        select.replaceChildren(new Option(placeholder, ""));
        rows.forEach(([id, name]) => select.add(new Option(name, id)));
        select.disabled = rows.length === 0;
    }
    function selectGroup() {
        const rows = catalog.filter(
            (e) => e.MuscleGroupId === Number(group.value),
        );
        options(
            subgroup,
            [
                ...new Map(
                    rows.map((e) => [e.MuscleSubGroupId, e.MuscleSubGroupName]),
                ),
            ],
            "All subgroups",
        );
        selectSubgroup();
    }
    function selectSubgroup() {
        const rows = catalog.filter(
            (e) =>
                e.MuscleGroupId === Number(group.value) &&
                (!subgroup.value ||
                    e.MuscleSubGroupId === Number(subgroup.value)),
        );
        options(
            exercise,
            rows.map((e) => [e.ExerciseId, e.ExerciseName]),
            "Choose exercise",
        );
        selectExercise();
    }
    function selectExercise() {
        const item = catalog.find(
            (e) => e.ExerciseId === Number(exercise.value),
        );
        const type = item?.ExerciseType ?? 0;
        get("weightContainer").hidden = type === 1;
        get("repsContainer").hidden = type !== 0;
        get("durationContainer").style.display = type !== 0 ? "" : "none";
        get("intensityContainer").style.display = type === 1 ? "" : "none";
        const last = draft.findLast(
            (s) => s.exerciseId === Number(exercise.value),
        );
        if (last) {
            get("weightInput").value = last.weightKg;
            get("repsInput").value = last.reps || 8;
            get("durationInput").value = last.durationSeconds || 60;
            get("intensityInput").value = last.intensity || 5;
            get("restInput").value = last.restSeconds;
            get("setTypeSelect").value = last.setTypeId;
        }
    }
    function render() {
        const body = get("draftSetsTable").tBodies[0];
        body.replaceChildren();
        draft.forEach((set, index) => {
            const row = body.insertRow();
            const type =
                catalog.find((e) => e.ExerciseId === set.exerciseId)
                    ?.ExerciseType ?? 0;
            const cells = [
                index + 1,
                catalog.find((e) => e.ExerciseId === set.exerciseId)
                    ?.ExerciseName || "Exercise",
                set.setTypeName,
                type ? `${set.durationSeconds} sec` : `${set.weightKg} kg`,
                type === 1
                    ? `${set.intensity}/10`
                    : type === 2
                      ? `${set.weightKg} kg`
                      : `${set.reps} reps`,
                `${set.restSeconds}s`,
            ];
            cells.forEach((value) => {
                row.insertCell().textContent = value;
            });
            const button = document.createElement("button");
            button.type = "button";
            button.className = "btn btn-sm btn-outline-danger";
            button.textContent = "×";
            button.setAttribute("aria-label", `Remove set ${index + 1}`);
            button.addEventListener("click", () => {
                draft.splice(index, 1);
                render();
            });
            row.insertCell().append(button);
        });
        get("draftCount").textContent = `${draft.length} sets logged`;
        get("emptyDraft").hidden = draft.length > 0;
        persist();
    }
    function tick() {
        const seconds = Math.max(0, Math.ceil((timerEnd - Date.now()) / 1000));
        get("restTimerValue").textContent =
            `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
        get("timerReadyText").textContent =
            timerEnd && seconds === 0 ? "Ready for your next set." : "";
    }
    function startTimer() {
        const seconds = Number(get("restInput").value);
        if (!Number.isInteger(seconds) || seconds < 0 || seconds > 3600) return;
        timerEnd = Date.now() + seconds * 1000;
        tick();
        persist();
    }
    function addSet() {
        const item = catalog.find(
            (e) => e.ExerciseId === Number(exercise.value),
        );
        const type = item?.ExerciseType;
        const setType = get("setTypeSelect");
        const weight = type === 1 ? 0 : Number(get("weightInput").value);
        const reps = type === 0 ? Number(get("repsInput").value) : 0;
        const duration = type !== 0 ? Number(get("durationInput").value) : 0;
        const intensity = type === 1 ? Number(get("intensityInput").value) : 0;
        const rest = Number(get("restInput").value);
        if (!item || !setType.value)
            return showError("Choose an exercise and set type first.");
        if (
            !Number.isFinite(weight) ||
            weight < 0 ||
            weight > 500 ||
            (type === 0 && (!Number.isInteger(reps) || reps < 1 || reps > 500))
        )
            return showError("Enter a load of 0–500 kg and 1–500 reps.");
        if (
            type !== 0 &&
            (!Number.isInteger(duration) || duration < 1 || duration > 86400)
        )
            return showError("Enter a duration of 1–86400 seconds.");
        if (
            type === 1 &&
            (!Number.isInteger(intensity) || intensity < 1 || intensity > 10)
        )
            return showError("Enter an intensity of 1–10.");
        if (!Number.isInteger(rest) || rest < 0 || rest > 3600)
            return showError("Rest must be 0–3600 seconds.");
        if (draft.length >= 200)
            return showError(
                "Save this workout before adding more than 200 sets.",
            );
        draft.push({
            exerciseId: item.ExerciseId,
            setTypeId: Number(setType.value),
            setTypeName: setType.selectedOptions[0].text,
            weightKg: weight,
            reps,
            durationSeconds: duration,
            intensity,
            restSeconds: rest,
        });
        showError("");
        render();
        startTimer();
        get("addSetBtn").textContent = "Set added ✓";
        setTimeout(() => (get("addSetBtn").textContent = "Add set ＋"), 1000);
    }
    function showError(message) {
        get("workoutError").textContent = message;
        get("workoutError").hidden = !message;
    }
    group.addEventListener("change", selectGroup);
    subgroup.addEventListener("change", selectSubgroup);
    exercise.addEventListener("change", selectExercise);
    get("addSetBtn").addEventListener("click", addSet);
    get("repeatSetBtn").addEventListener("click", addSet);
    get("startTimerBtn").addEventListener("click", startTimer);
    get("skipTimerBtn").addEventListener("click", () => {
        timerEnd = Date.now();
        tick();
        persist();
    });
    form.addEventListener("submit", (event) => {
        if (draft.length === 0) {
            event.preventDefault();
            showError("Add a set before finishing your workout.");
            return;
        }
        const minutes = Number(form.elements.DurationMinutes.value);
        if (!Number.isInteger(minutes) || minutes < 1 || minutes > 600) {
            event.preventDefault();
            showError("Enter a workout duration of 1–600 minutes.");
            return;
        }
        if (!form.elements.SplitDayLabel.value) {
            event.preventDefault();
            showError("Choose a training day.");
            return;
        }
        saved = true;
        get("finishWorkoutBtn").disabled = true;
        get("finishWorkoutBtn").textContent = "Saving workout…";
    });
    window.addEventListener("beforeunload", (event) => {
        if (!saved && draft.length) {
            event.preventDefault();
            event.returnValue = "";
        }
    });
    const lastExercise = catalog.find(
        (e) => e.ExerciseId === draft.at(-1)?.exerciseId,
    );
    if (lastExercise) group.value = lastExercise.MuscleGroupId;
    selectGroup();
    if (lastExercise) {
        exercise.value = lastExercise.ExerciseId;
        selectExercise();
    }
    render();
    tick();
    setInterval(tick, 500);
})();
