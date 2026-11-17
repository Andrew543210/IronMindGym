"use strict";
(() => {
    const get = (id) => document.getElementById(id);
    const form = get("finishWorkoutForm");
    if (!form) return;
    const catalog = JSON.parse(get("exerciseCatalog").textContent);
    const group = get("muscleGroupFilter"),
        subgroup = get("muscleSubGroupFilter"),
        exercise = get("exerciseSelect");
    const payload = get("draftPayloadJson");
    const storageKey = `ironmind-workout-${form.dataset.userId}`;
    const settings = [
        "SplitDayLabel",
        "DurationMinutes",
        "ReviewRating",
        "ReviewComment",
    ];
    const defaultSetType = get("setTypeSelect").value;
    let draft = [],
        timerEnd = 0,
        saved = false,
        editingIndex = null;
    let startedAt = Date.now(),
        previousSet = null,
        previousRequest = 0;
    let stored = null;
    try {
        stored = JSON.parse(sessionStorage.getItem(storageKey) || "null");
    } catch {
        /* Storage may be unavailable. */
    }
    try {
        const serverDraft = JSON.parse(payload.value || "[]");
        draft = serverDraft.length ? serverDraft : stored?.sets || [];
        if (!Array.isArray(draft)) draft = [];
    } catch {
        draft = [];
    }
    if (stored && draft.length > 0) {
        timerEnd = stored.timerEnd || 0;
        if (Number.isFinite(stored.startedAt) && stored.startedAt <= Date.now())
            startedAt = stored.startedAt;
        if (payload.value === "[]") {
            settings.forEach((name) => {
                if (stored.settings?.[name] != null)
                    form.elements[name].value = stored.settings[name];
            });
        }
        get("autoDuration").checked = stored.autoDuration !== false;
    }

    function persist() {
        payload.value = JSON.stringify(draft);
        try {
            sessionStorage.setItem(
                storageKey,
                JSON.stringify({
                    sets: draft,
                    timerEnd,
                    startedAt,
                    autoDuration: get("autoDuration").checked,
                    settings: Object.fromEntries(
                        settings.map((name) => [
                            name,
                            form.elements[name].value,
                        ]),
                    ),
                }),
            );
            get("draftStatus").textContent = "Draft saved in this tab";
        } catch {
            get("draftStatus").textContent =
                "Draft storage unavailable — keep this page open";
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
    function fillSet(set) {
        get("weightInput").value = set.weightKg;
        get("repsInput").value = set.reps || 8;
        get("durationInput").value = set.durationSeconds || 60;
        get("intensityInput").value = set.intensity || 5;
        get("restInput").value = set.restSeconds;
        get("setTypeSelect").value = set.setTypeId;
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
        fillSet(
            last || {
                weightKg: 0,
                reps: 8,
                durationSeconds: 60,
                intensity: 5,
                restSeconds: 120,
                setTypeId: defaultSetType,
            },
        );
        loadPrevious(item);
    }
    function chooseExercise(item) {
        group.value = item.MuscleGroupId;
        selectGroup();
        exercise.value = item.ExerciseId;
        selectExercise();
    }
    async function loadPrevious(item) {
        const request = ++previousRequest;
        previousSet = null;
        get("usePreviousSet").hidden = true;
        get("previousSetText").textContent = item
            ? "Loading your previous set…"
            : "Choose an exercise to see your previous set.";
        if (!item) return;
        try {
            const response = await fetch(
                `${form.dataset.previousUrl}?exerciseId=${item.ExerciseId}`,
                { headers: { Accept: "application/json" } },
            );
            if (request !== previousRequest) return;
            if (response.status === 204) {
                get("previousSetText").textContent =
                    "No previous set for this exercise. Start with your own values.";
                return;
            }
            if (!response.ok) throw new Error("Previous set unavailable");
            const result = await response.json();
            if (request !== previousRequest) return;
            previousSet = result;
            const description =
                item.ExerciseType === 0
                    ? `${result.weightKg} kg × ${result.reps} reps`
                    : `${result.durationSeconds} sec · ${item.ExerciseType === 1 ? `effort ${result.intensity}/10` : `${result.weightKg} kg`}`;
            get("previousSetText").textContent =
                `Last recorded set (${result.date.slice(0, 10)}): ${description}. Reference only — adjust for today.`;
            get("usePreviousSet").hidden = false;
        } catch {
            if (request === previousRequest)
                get("previousSetText").textContent =
                    "Previous set unavailable. You can still log this workout.";
        }
    }
    function searchExercises() {
        const query = get("exerciseSearch").value.trim().toLowerCase();
        const results = get("exerciseSearchResults");
        results.replaceChildren();
        if (!query) return;
        const matches = catalog
            .filter((e) => e.ExerciseName.toLowerCase().includes(query))
            .slice(0, 8);
        if (!matches.length)
            results.textContent =
                "No matches. Try another name or browse a muscle group.";
        matches.forEach((item) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "exercise-search-result";
            button.textContent = `${item.ExerciseName} · ${item.MuscleGroupName}`;
            button.addEventListener("click", () => {
                chooseExercise(item);
                results.replaceChildren();
                get("exerciseSearch").value = "";
                exercise.focus();
            });
            results.append(button);
        });
    }
    function endEdit() {
        editingIndex = null;
        get("addSetBtn").textContent = "Log completed set ＋";
        get("cancelEditBtn").hidden = true;
    }
    function editSet(index) {
        const set = draft[index];
        const item = catalog.find((e) => e.ExerciseId === set.exerciseId);
        if (!item)
            return showError(
                "This exercise is no longer available. Remove the set and choose another exercise.",
            );
        editingIndex = index;
        chooseExercise(item);
        fillSet(set);
        get("addSetBtn").textContent = `Save changes to set ${index + 1}`;
        get("cancelEditBtn").hidden = false;
        get("setEditor").scrollIntoView({ behavior: "smooth", block: "start" });
        get(item.ExerciseType === 0 ? "weightInput" : "durationInput").focus({
            preventScroll: true,
        });
    }
    function render() {
        const body = get("draftSetsTable").tBodies[0];
        body.replaceChildren();
        draft.forEach((set, index) => {
            const row = body.insertRow();
            const item = catalog.find((e) => e.ExerciseId === set.exerciseId);
            const type = item?.ExerciseType ?? 0;
            const cells = [
                index + 1,
                item?.ExerciseName || "Exercise",
                set.setTypeName,
                type ? `${set.durationSeconds} sec` : `${set.weightKg} kg`,
                type === 1
                    ? `${set.intensity}/10 effort`
                    : type === 2
                      ? `${set.weightKg} kg`
                      : `${set.reps} reps`,
                `${set.restSeconds}s`,
            ];
            cells.forEach((value) => {
                row.insertCell().textContent = value;
            });
            const actions = row.insertCell();
            const edit = document.createElement("button");
            edit.type = "button";
            edit.className = "btn btn-sm btn-outline-secondary";
            edit.textContent = "Edit";
            edit.setAttribute("aria-label", `Edit set ${index + 1}`);
            edit.addEventListener("click", () => editSet(index));
            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "btn btn-sm btn-outline-danger";
            remove.textContent = "×";
            remove.setAttribute("aria-label", `Remove set ${index + 1}`);
            remove.addEventListener("click", () => {
                draft.splice(index, 1);
                endEdit();
                render();
            });
            actions.append(edit, remove);
        });
        get("draftCount").textContent = `${draft.length} sets logged`;
        get("emptyDraft").hidden = draft.length > 0;
        persist();
    }
    function clockText(seconds) {
        return `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
    }
    function tick() {
        const seconds = Math.max(0, Math.ceil((timerEnd - Date.now()) / 1000));
        get("restTimerValue").textContent = clockText(seconds);
        get("timerReadyText").textContent =
            timerEnd && seconds === 0
                ? "Ready when you are. Adjust rest to your session."
                : "";
        const elapsed = Math.max(
            0,
            Math.floor((Date.now() - startedAt) / 1000),
        );
        get("sessionElapsed").textContent = clockText(elapsed);
        if (get("autoDuration").checked)
            form.elements.DurationMinutes.value = Math.max(
                1,
                Math.ceil(elapsed / 60),
            );
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
            (type !== 1 && !get("weightInput").value.trim()) ||
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
            return showError(
                "Enter perceived effort from 1 (very easy) to 10 (maximal).",
            );
        if (!Number.isInteger(rest) || rest < 0 || rest > 3600)
            return showError("Rest must be 0–3600 seconds.");
        if (editingIndex === null && draft.length >= 200)
            return showError(
                "Save this workout before adding more than 200 sets.",
            );
        const set = {
            exerciseId: item.ExerciseId,
            setTypeId: Number(setType.value),
            setTypeName: setType.selectedOptions[0].text,
            weightKg: weight,
            reps,
            durationSeconds: duration,
            intensity,
            restSeconds: rest,
        };
        const wasEditing = editingIndex !== null;
        if (wasEditing) draft[editingIndex] = set;
        else draft.push(set);
        endEdit();
        showError("");
        render();
        if (!wasEditing) startTimer();
        get("draftStatus").textContent = wasEditing
            ? "Set updated · draft saved"
            : "Set logged · draft saved";
    }
    function showError(message) {
        get("workoutError").textContent = message;
        get("workoutError").hidden = !message;
    }
    group.addEventListener("change", selectGroup);
    subgroup.addEventListener("change", selectSubgroup);
    exercise.addEventListener("change", selectExercise);
    get("exerciseSearch").addEventListener("input", searchExercises);
    get("exerciseSearch").addEventListener("keydown", (event) => {
        if (event.key === "Enter") event.preventDefault();
    });
    get("usePreviousSet").addEventListener("click", () => {
        if (previousSet) fillSet(previousSet);
    });
    get("addSetBtn").addEventListener("click", addSet);
    get("cancelEditBtn").addEventListener("click", endEdit);
    get("startTimerBtn").addEventListener("click", startTimer);
    get("skipTimerBtn").addEventListener("click", () => {
        timerEnd = Date.now();
        tick();
        persist();
    });
    settings.forEach((name) =>
        form.elements[name].addEventListener("input", () => {
            if (name === "DurationMinutes") get("autoDuration").checked = false;
            persist();
        }),
    );
    get("autoDuration").addEventListener("change", () => {
        tick();
        persist();
    });
    form.addEventListener("submit", (event) => {
        let error = "";
        const minutes = Number(form.elements.DurationMinutes.value);
        if (editingIndex !== null)
            error = "Save or cancel your set edits before finishing.";
        else if (draft.length === 0)
            error = "Add a set before finishing your workout.";
        else if (!Number.isInteger(minutes) || minutes < 1 || minutes > 600)
            error =
                "Enter a workout duration of 1–600 minutes in Session settings.";
        else if (!form.elements.SplitDayLabel.value)
            error = "Choose a training day in Session settings.";
        if (error) {
            event.preventDefault();
            showError(error);
            get("workoutError").scrollIntoView({ block: "center" });
            return;
        }
        persist();
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
    window.addEventListener("pageshow", (event) => {
        if (!event.persisted) return;
        try {
            if (saved && !sessionStorage.getItem(storageKey)) {
                draft = [];
                startedAt = Date.now();
                timerEnd = 0;
                endEdit();
                render();
            }
        } catch {
            /* Keep the visible draft when storage is unavailable. */
        }
        saved = false;
        get("finishWorkoutBtn").disabled = false;
        get("finishWorkoutBtn").textContent = "Finish workout";
    });
    const lastExercise = catalog.find(
        (e) => e.ExerciseId === draft.at(-1)?.exerciseId,
    );
    if (lastExercise) chooseExercise(lastExercise);
    else selectGroup();
    tick();
    render();
    setInterval(tick, 500);
})();
