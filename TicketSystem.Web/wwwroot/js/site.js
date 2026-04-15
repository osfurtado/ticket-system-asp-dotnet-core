// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.



document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // 1. Sidebar toggle
    // ==========================================
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.toggle("collapsed");
        });
    }

    // ==========================================
    // 2. Theme (Light / Dark)
    // ==========================================
    const themeToggle = document.getElementById('themeToggle');
    const themeIcon = document.getElementById('themeIcon');

    if (themeToggle && themeIcon) {
        function applyTheme(theme) {
            document.documentElement.setAttribute('data-bs-theme', theme);
            themeIcon.className = theme === 'dark' ? 'bi bi-sun fs-5' : 'bi bi-moon fs-5';
            localStorage.setItem('theme', theme);
        }


        applyTheme(localStorage.getItem('theme') || 'light');


        themeToggle.addEventListener('click', () => {
            const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
            const newTheme = isDark ? 'light' : 'dark';
            applyTheme(newTheme);
        });
    }

    // ==========================================
    // 3. Lógica do Menu Ativo Dinâmico
    // ==========================================
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('#sidebar .nav-link');

    navLinks.forEach(link => {
        // Pega o href do link e converte para minúsculas
        const linkPath = link.getAttribute('href')?.toLowerCase();

        if (linkPath) {
            // Se a rota atual bater com o href do link, adiciona active
            // A segunda condição previne que a root "/" não marque nada
            if (currentPath === linkPath || (currentPath === '/' && linkPath.includes('/home'))) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        }
    });
});