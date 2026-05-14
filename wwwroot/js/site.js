(function () {
  const themeKey = 'shopping-list-theme';
  const themeToggle = document.getElementById('themeToggle');
  const themeToggleIcon = document.querySelector('.theme-toggle-icon');
  const themeToggleText = document.querySelector('.theme-toggle-text');

  function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem(themeKey, theme);

    if (themeToggleIcon && themeToggleText) {
      const isDark = theme === 'dark';
      themeToggleIcon.textContent = isDark ? '☀️' : '🌙';
      themeToggleText.textContent = isDark ? 'Светлая тема' : 'Тёмная тема';
    }
  }

  applyTheme(localStorage.getItem(themeKey) || 'light');

  themeToggle?.addEventListener('click', function () {
    const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
    applyTheme(currentTheme === 'dark' ? 'light' : 'dark');
  });

  function syncCustomUnit(select) {
    const targetSelector = select.getAttribute('data-custom-unit-target');
    if (!targetSelector) {
      return;
    }

    const container = document.querySelector(targetSelector);
    if (!container) {
      return;
    }

    const input = container.querySelector('input');
    const isOther = select.value === 'другое';
    container.classList.toggle('d-none', !isOther);

    if (input) {
      input.disabled = !isOther;
      if (!isOther) {
        input.value = '';
      }
    }
  }

  document.querySelectorAll('[data-unit-select]').forEach(function (select) {
    syncCustomUnit(select);
    select.addEventListener('change', function () {
      syncCustomUnit(select);
    });
  });
})();
