"use strict";
let progressCatalog = [],
    progressExercise = null;
const trackerElement = (id) => document.getElementById(id);

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
            result?.message || "Could not save your results. Please try again.",
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
    trackerElement("progressContainer").style.display = "none";
}

async function selectExercise() {
    progressExercise = progressCatalog.find(
        (e) => e.exerciseId === Number(trackerElement("exerciseSelect").value),
    );
    trackerElement("progressContainer").style.display = progressExercise
        ? ""
        : "none";
    if (!progressExercise) return;
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
        first.disabled = false;
        second.disabled = false;
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
        if (progressExercise?.exerciseId !== selectedId) return;
        if (data.hasData) {
            [
                "initialValue1",
                "initialValue2",
                "currentValue1",
                "currentValue2",
            ].forEach((id) => (trackerElement(id).value = data[id]));
        }
        updateProgressDisplay();
        trackerMessage("");
    } catch (error) {
        trackerMessage(error.message, true);
    }
}

function updateProgressDisplay() {
    const initial = Number(trackerElement("initialValue1").value),
        current = Number(trackerElement("currentValue1").value);
    const display = trackerElement("progressDisplay");
    if (!initial || !current) {
        display.textContent =
            "Save your baseline and latest result to compare them.";
        return;
    }
    const difference = (((current - initial) / initial) * 100).toFixed(1);
    display.textContent = `${difference > 0 ? "+" : ""}${difference}% ${progressExercise?.exerciseType === 0 ? "load change" : "duration change"}`;
}

async function setResult() {
    if (!progressExercise) return;
    const button = trackerElement("progressContainer").querySelector("button");
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
        for (const prefix of ["initial", "current"]) {
            const data = new FormData();
            data.append("exerciseId", progressExercise.exerciseId);
            const first = trackerElement(prefix + "Value1").value,
                second = trackerElement(prefix + "Value2").value;
            if (progressExercise.exerciseType === 0) {
                data.append("weightKg", first);
                data.append("reps", second);
            } else {
                data.append("durationSeconds", first);
                data.append(
                    progressExercise.exerciseType === 1
                        ? "intensity"
                        : "weightKg",
                    second,
                );
            }
            await progressRequest(
                `/ProgressTracker/Set${prefix === "initial" ? "Initial" : "Current"}Results`,
                data,
            );
        }
        updateProgressDisplay();
        trackerMessage("Results saved. Your next session starts from here.");
    } catch (error) {
        trackerMessage(error.message, true);
    } finally {
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
        [
            "initialValue1",
            "initialValue2",
            "currentValue1",
            "currentValue2",
        ].forEach((id) =>
            trackerElement(id).addEventListener("input", updateProgressDisplay),
        );
    } catch (error) {
        trackerMessage(error.message, true);
    }
})();
