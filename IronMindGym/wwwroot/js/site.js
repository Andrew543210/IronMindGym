"use strict";
const moreButton = document.getElementById("moreNavigation");
const moreMenu = document.getElementById("moreMenu");
moreButton?.addEventListener("click", () => {
    moreMenu.hidden = !moreMenu.hidden;
    moreButton.setAttribute("aria-expanded", String(!moreMenu.hidden));
});
document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && moreMenu) {
        moreMenu.hidden = true;
        moreButton.setAttribute("aria-expanded", "false");
    }
});
document.querySelectorAll("table").forEach((table) => {
    if (table.parentElement.classList.contains("table-responsive")) return;
    const wrapper = document.createElement("div");
    wrapper.className = "table-responsive";
    table.before(wrapper);
    wrapper.append(table);
});
