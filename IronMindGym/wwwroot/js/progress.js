"use strict";
let progressCatalog = [],
    progressExercise = null,
    selectionVersion = 0;
const trackerElement = (id) => document.getElementById(id);
const resultFields = [
    "initialValue1",
    "initialValue2",
    "currentValue1",
    "currentValue2",
];

function trackerMessage(message, error = false) {
    const box = trackerElement("trackerMessage");
    box.textContent = message;
    box.hidden = !message;
    box.className = `alert mt-4 ${error ? "alert-danger" : "alert-success"}`;
}

async function progressRequest(url, data) {
    const response = await fetch(
        url,
        data
            ? {
                  method: "POST",
                  body: data,
                  headers: {
                      "X-CSRF-TOKEN": document.querySelector(
                          'input[name="__RequestVerificationToken"]',
                      ).value,
                  },
              }
            : {},
    );
    if (response.redirected)
        throw new Error("Your session expired. Sign in again before saving.");
    const result = await response.json().catch(() => null);
    if (!response.ok || !result || result.success === false)
        throw new Error(
            result?.message ||
                (data
                    ? "Could not save your results. Please try again."
                    : "Could not load your saved results."),
        );
    return result;
}

function fillTrackerOptions(id, values, placeholder) {
    const select = trackerElement(id);
    select.replaceChildren(new Option(placeholder, ""));
    values.forEach(([value, title]) => select.add(new Option(title, value)));
    select.disabled = values.length === 0;
}

function loadSubgroups() {
    const group = trackerElement("muscleGroupSelect").value;
    const rows = progressCatalog.filter((e) => e.muscleGroupName === group);
    fillTrackerOptions(
        "muscleSubgroupSelect",
        [...new Set(rows.map((e) => e.muscleSubGroupName))].map((n) => [n, n]),
        "All subgroups",
    );
    loadExercises();
}

function loadExercises() {
    const group = trackerElement("muscleGroupSelect").value,
        subgroup = trackerElement("muscleSubgroupSelect").value;
    const rows = progressCatalog.filter(
        (e) =>
            e.muscleGroupName === group &&
            (!subgroup || e.muscleSubGroupName === subgroup),
    );
    fillTrackerOptions(
        "exerciseSelect",
        rows.map((e) => [e.exerciseId, e.exerciseName]),
        "Choose exercise",
    );
    selectExercise();
}

async function selectExercise() {
    const version = ++selectionVersion;
    progressExercise = progressCatalog.find(
        (e) => e.exerciseId === Number(trackerElement("exerciseSelect").value),
    );
    trackerElement("progressContainer").style.display = progressExercise
        ? ""
        : "none";
    trackerMessage("");
    if (!progressExercise) return;
    const button = trackerElement("progressContainer").querySelector("button");
    button.disabled = true;
    trackerElement("progressDisplay").textContent = "Loading saved results…";
    const selectedId = progressExercise.exerciseId,
        type = progressExercise.exerciseType;
    ["initial", "current"].forEach((prefix) => {
        trackerElement(prefix + "Label").textContent =
            type === 0 ? "Load (kg)" : "Duration (seconds)";
        trackerElement(prefix + "Label2").textContent =
            type === 0
                ? "Reps"
                : type === 1
                  ? "Intensity (1–10)"
                  : "External load (kg)";
        const first = trackerElement(prefix + "Value1"),
            second = trackerElement(prefix + "Value2");
        first.value = "";
        second.value = "";
        first.disabled = true;
        second.disabled = true;
        first.placeholder = type === 0 ? "kg" : "Seconds";
        second.placeholder = type === 0 ? "Reps" : type === 1 ? "1–10" : "kg";
        first.min = type === 0 ? 0 : 1;
        first.max = type === 0 ? 500 : 86400;
        first.step = type === 0 ? ".5" : "1";
        second.min = type === 2 ? 0 : 1;
        second.max = type === 1 ? 10 : 500;
        second.step = type === 2 ? ".5" : "1";
    });
    try {
        const data = await progressRequest(
            `/ProgressTracker/GetExerciseProgress?exerciseId=${selectedId}`,
        );
        if (version !== selectionVersion) return;
        resultFields.forEach((id) => {
            const field = trackerElement(id);
            if (data.hasData) field.value = data[id];
            field.disabled = false;
        });
        button.disabled = false;
        updateProgressDisplay();
    } catch (error) {
        if (version !== selectionVersion) return;
        trackerElement("progressDisplay").textContent =
            "Saved results are unavailable.";
        trackerMessage(error.message, true);
        const retry = document.createElement("button");
        retry.type = "button";
        retry.className = "btn btn-outline-secondary btn-sm ms-2";
        retry.textContent = "Retry";
        retry.addEventListener("click", selectExercise);
        trackerElement("trackerMessage").append(retry);
    }
}

function updateProgressDisplay() {
    const fields = resultFields.map(trackerElement);
    const display = trackerElement("progressDisplay");
    if (fields.some((field) => field.value === "" || !field.checkValidity())) {
        display.textContent =
            "Enter your baseline and latest result to compare them.";
        return;
    }
    const [initial, initialSecond, current, currentSecond] = fields.map(
        (field) => Number(field.value),
    );
    const signed = (value) =>
        `${value > 0 ? "+" : ""}${Number(value.toFixed(1))}`;
    const type = progressExercise?.exerciseType;
    const firstUnit = type === 0 ? "kg load" : "sec duration";
    const secondUnit =
        type === 0 ? "reps" : type === 1 ? "effort points" : "kg load";
    display.textContent = `${signed(current - initial)} ${firstUnit}; ${signed(currentSecond - initialSecond)} ${secondUnit}. Compare under similar conditions.`;
}

async function setResult() {
    if (!progressExercise) return;
    const button = trackerElement("progressContainer").querySelector("button");
    if (button.disabled) return;
    const controls = [
        ...document.querySelectorAll(
            "#progressContainer input, #muscleGroupSelect, #muscleSubgroupSelect, #exerciseSelect",
        ),
    ];
    const disabledStates = controls.map((control) => control.disabled);
    button.disabled = true;
    try {
        for (const prefix of ["initial", "current"]) {
            const first = trackerElement(prefix + "Value1"),
                second = trackerElement(prefix + "Value2");
            if (
                !first.value ||
                second.value === "" ||
                !first.checkValidity() ||
                !second.checkValidity()
            )
                throw new Error(
                    "Enter valid baseline and current results in both columns.",
                );
        }
        const data = new FormData();
        data.append("exerciseId", progressExercise.exerciseId);
        for (const id of resultFields)
            data.append(id, trackerElement(id).value);
        controls.forEach((control) => (control.disabled = true));
        await progressRequest("/ProgressTracker/SaveResults", data);
        updateProgressDisplay();
        trackerMessage("Results saved. Your next session starts from here.");
    } catch (error) {
        trackerMessage(error.message, true);
    } finally {
        controls.forEach(
            (control, index) => (control.disabled = disabledStates[index]),
        );
        button.disabled = false;
    }
}

(async () => {
    try {
        const response = await fetch("/ProgressTracker/GetExerciseCatalog");
        if (!response.ok || response.redirected)
            throw new Error(
                "Could not load the exercise library. Sign in again or reload the page.",
            );
        progressCatalog = await response.json();
        fillTrackerOptions(
            "muscleGroupSelect",
            [...new Set(progressCatalog.map((e) => e.muscleGroupName))]
                .sort()
                .map((n) => [n, n]),
            "Choose muscle group",
        );
        const selected = progressCatalog.find(
            (e) =>
                e.exerciseId ===
                Number(trackerElement("trackerConfig").dataset.exerciseId),
        );
        if (selected) {
            trackerElement("muscleGroupSelect").value =
                selected.muscleGroupName;
            loadSubgroups();
            trackerElement("exerciseSelect").value = selected.exerciseId;
            await selectExercise();
        }
        resultFields.forEach((id) =>
            trackerElement(id).addEventListener("input", updateProgressDisplay),
        );
    } catch (error) {
        trackerMessage(error.message, true);
    }
})();
