document.addEventListener("DOMContentLoaded", function () {
    const html = document.documentElement;
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    const themeToggle = document.getElementById("themeToggle");

    const savedTheme = localStorage.getItem("theme");
    if (savedTheme) {
        html.setAttribute("data-theme", savedTheme);
    } else {
        html.setAttribute("data-theme", "light");
    }

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.toggle("show");
        });
    }

    if (themeToggle) {
        themeToggle.addEventListener("click", function () {
            const currentTheme = html.getAttribute("data-theme") || "light";
            const newTheme = currentTheme === "light" ? "dark" : "light";

            html.setAttribute("data-theme", newTheme);
            localStorage.setItem("theme", newTheme);
        });
    }
});