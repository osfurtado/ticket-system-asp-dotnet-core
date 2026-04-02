// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.



document.addEventListener("DOMContentLoaded", function () {
    // 1. Sidebar Toggle
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");

    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.toggle("collapsed");
        });
    }

    // 2. Theme Toggle (Light/Dark mode do Bootstrap 5)
    const themeToggle = document.getElementById("themeToggle");
    const themeIcon = document.getElementById("themeIcon");
    const htmlElement = document.documentElement;

    if (themeToggle) {
        themeToggle.addEventListener("click", function () {
            const currentTheme = htmlElement.getAttribute("data-bs-theme");
            if (currentTheme === "dark") {
                htmlElement.setAttribute("data-bs-theme", "light");
                themeIcon.classList.replace("bi-sun", "bi-moon");
            } else {
                htmlElement.setAttribute("data-bs-theme", "dark");
                themeIcon.classList.replace("bi-moon", "bi-sun");
            }
        });
    }
});