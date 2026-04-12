function refreshPdf() {
  const frame = document.querySelector('.pdf-frame');
  if (frame) frame.src = frame.src;
}

function normalize(value) {
  return (value || '').trim().toLowerCase();
}

function parseCellValue(raw, typeHint) {
  const value = (raw || '').trim();
  if (typeHint === 'number') {
    const num = Number(value.replace(/[^0-9.-]/g, ''));
    return Number.isNaN(num) ? 0 : num;
  }
  if (typeHint === 'date') {
    const timestamp = Date.parse(value);
    return Number.isNaN(timestamp) ? 0 : timestamp;
  }
  const maybeDate = Date.parse(value);
  if (!Number.isNaN(maybeDate) && /\d{4}-\d{2}-\d{2}|UTC|\d{2}:\d{2}/i.test(value)) return maybeDate;
  const maybeNumber = Number(value.replace(/[^0-9.-]/g, ''));
  if (!Number.isNaN(maybeNumber) && /\d/.test(value)) return maybeNumber;
  return value.toLowerCase();
}

function initTableSorting() {
  document.querySelectorAll('table[data-sortable]').forEach((table) => {
    const body = table.querySelector('tbody');
    if (!body) return;

    table.querySelectorAll('.sort-btn').forEach((button) => {
      button.setAttribute('aria-sort', 'none');
      button.addEventListener('click', () => {
        const colIndex = Number(button.dataset.sortCol || 0);
        const sortType = button.dataset.sortType || '';
        const current = button.getAttribute('aria-sort');
        const next = current === 'ascending' ? 'descending' : 'ascending';

        table.querySelectorAll('.sort-btn').forEach((b) => {
          if (b !== button) b.setAttribute('aria-sort', 'none');
        });
        button.setAttribute('aria-sort', next);

        const rows = Array.from(body.querySelectorAll('tr'));
        rows.sort((a, b) => {
          const va = parseCellValue(a.children[colIndex]?.innerText || '', sortType);
          const vb = parseCellValue(b.children[colIndex]?.innerText || '', sortType);
          if (va > vb) return next === 'ascending' ? 1 : -1;
          if (va < vb) return next === 'ascending' ? -1 : 1;
          return 0;
        });

        rows.forEach((row) => body.appendChild(row));
      });
    });
  });
}

function initDensityToggle() {
  document.querySelectorAll('[data-density-target]').forEach((button) => {
    button.addEventListener('click', () => {
      const target = document.getElementById(button.dataset.densityTarget || '');
      if (!target) return;
      const compact = target.classList.toggle('compact');
      button.setAttribute('aria-pressed', compact ? 'true' : 'false');
      button.textContent = compact ? 'Comfortable Rows' : 'Compact Rows';
    });
  });
}

function initComparisonScreen() {
  const layoutSelect = document.getElementById('layoutSelect');
  const layout = document.getElementById('comparisonLayout');
  const rows = Array.from(document.querySelectorAll('.comparison-row'));
  const mismatchCount = document.getElementById('mismatchCount');
  const visibleMismatchCount = document.getElementById('visibleMismatchCount');
  const submitBtn = document.getElementById('submitBtn');
  const mismatchOnlyToggle = document.getElementById('mismatchOnlyToggle');
  const nextMismatchBtn = document.getElementById('nextMismatchBtn');

  if (layoutSelect && layout) {
    layoutSelect.addEventListener('change', () => {
      layout.className = `comparison-layout ${layoutSelect.value}`;
    });
  }

  if (rows.length === 0) return;

  const evaluate = () => {
    const mismatchOnly = mismatchOnlyToggle ? mismatchOnlyToggle.checked : false;
    let mismatches = 0;
    let visibleMismatches = 0;
    const mismatchRows = [];

    rows.forEach((row) => {
      const expected = normalize(row.dataset.expected);
      const input = row.querySelector('input[name$="ActualValue"]');
      const actual = normalize(input ? input.value : '');
      const isBlocking = row.dataset.blocking === 'true';
      const indicator = row.querySelector('.match-indicator');
      const match = expected === actual;

      row.classList.toggle('mismatch-row', !match);
      row.classList.toggle('hidden-by-filter', mismatchOnly && match);

      if (!match) {
        visibleMismatches++;
        mismatchRows.push(row);
      }

      if (indicator) {
        indicator.textContent = match ? 'Match' : 'Mismatch';
        indicator.className = `match-indicator ${match ? 'ok' : 'bad'}`;
      }

      if (!match && isBlocking) mismatches++;
    });

    if (mismatchCount) mismatchCount.textContent = String(mismatches);
    if (visibleMismatchCount) visibleMismatchCount.textContent = String(visibleMismatches);
    if (submitBtn) submitBtn.disabled = mismatches > 0;

    return mismatchRows;
  };

  rows.forEach((row) => {
    const input = row.querySelector('input[name$="ActualValue"]');
    if (input) input.addEventListener('input', evaluate);
  });

  if (mismatchOnlyToggle) mismatchOnlyToggle.addEventListener('change', evaluate);

  if (nextMismatchBtn) {
    nextMismatchBtn.addEventListener('click', () => {
      const mismatchRows = evaluate();
      const target = mismatchRows.find((x) => !x.classList.contains('hidden-by-filter')) || mismatchRows[0];
      if (!target) return;
      target.scrollIntoView({ behavior: 'smooth', block: 'center' });
      target.classList.remove('mismatch-focus');
      requestAnimationFrame(() => target.classList.add('mismatch-focus'));
    });
  }

  evaluate();
}

function initPasswordToggle() {
  document.querySelectorAll('[data-password-toggle]').forEach((button) => {
    button.addEventListener('click', () => {
      const input = document.getElementById(button.dataset.passwordToggle || '');
      if (!input) return;
      const isPassword = input.type === 'password';
      input.type = isPassword ? 'text' : 'password';
      button.textContent = isPassword ? 'Hide' : 'Show';
    });
  });
}

function initSidebarToggle() {
  const toggle = document.getElementById('sidebarToggle');
  if (!toggle) return;
  toggle.addEventListener('click', () => {
    document.body.classList.toggle('sidebar-collapsed');
  });
}

function initSidebarSearch() {
  const search = document.getElementById('sidebarSearch');
  if (!search) return;

  search.addEventListener('input', () => {
    const query = normalize(search.value);
    document.querySelectorAll('[data-nav-item]').forEach((item) => {
      const text = normalize(item.getAttribute('data-nav-item'));
      item.style.display = text.includes(query) ? '' : 'none';
    });
  });
}

function initDropdowns() {
  document.querySelectorAll('[data-dropdown-target]').forEach((trigger) => {
    trigger.addEventListener('click', (event) => {
      event.preventDefault();
      const id = trigger.getAttribute('data-dropdown-target');
      if (!id) return;
      const target = document.getElementById(id);
      if (!target) return;

      const isHidden = target.hasAttribute('hidden');
      document.querySelectorAll('.dropdown-card').forEach((el) => el.setAttribute('hidden', 'hidden'));
      if (isHidden) target.removeAttribute('hidden');
    });
  });

  document.addEventListener('click', (event) => {
    if (event.target.closest('[data-dropdown-target]') || event.target.closest('.dropdown-card')) return;
    document.querySelectorAll('.dropdown-card').forEach((el) => el.setAttribute('hidden', 'hidden'));
  });
}

function initThemeCustomizer() {
  const root = document.documentElement;
  const customizer = document.getElementById('themeCustomizer');
  const toggle = document.getElementById('customizerToggle');
  const close = document.querySelector('[data-customizer-close]');

  if (toggle && customizer) {
    toggle.addEventListener('click', () => customizer.classList.toggle('open'));
  }
  if (close && customizer) {
    close.addEventListener('click', () => customizer.classList.remove('open'));
  }

  const applyTheme = (key, value) => {
    if (key === 'sidebar') {
      document.body.classList.toggle('sidebar-collapsed', value === 'collapsed');
    }
    if (key === 'header') {
      root.setAttribute('data-header', value);
    }
    if (key === 'mode') {
      if (value === 'system') {
        const dark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        root.setAttribute('data-theme', dark ? 'dark' : 'light');
      } else {
        root.setAttribute('data-theme', value);
      }
    }
    if (key === 'dir') {
      root.setAttribute('dir', value);
    }

    localStorage.setItem('admin_theme', JSON.stringify({
      sidebar: document.body.classList.contains('sidebar-collapsed') ? 'collapsed' : 'expanded',
      header: root.getAttribute('data-header') || 'sticky',
      mode: root.getAttribute('data-theme') || 'light',
      dir: root.getAttribute('dir') || 'ltr'
    }));
  };

  document.querySelectorAll('[data-theme-set]').forEach((button) => {
    button.addEventListener('click', () => {
      const key = button.getAttribute('data-theme-set');
      const value = button.getAttribute('data-theme-value');
      if (!key || !value) return;
      applyTheme(key, value);

      document.querySelectorAll(`[data-theme-set="${key}"]`).forEach((x) => x.classList.remove('active'));
      button.classList.add('active');
    });
  });

  const saved = localStorage.getItem('admin_theme');
  if (saved) {
    try {
      const state = JSON.parse(saved);
      applyTheme('sidebar', state.sidebar || 'expanded');
      applyTheme('header', state.header || 'sticky');
      applyTheme('mode', state.mode || 'light');
      applyTheme('dir', state.dir || 'ltr');
    } catch {
      // ignore malformed state
    }
  }
}

function initConfirmModal() {
  const modal = document.getElementById('confirmModal');
  if (!modal) return;

  const modalMessage = document.getElementById('confirmModalMessage');
  const confirmOk = modal.querySelector('[data-confirm-ok]');
  const confirmCancel = modal.querySelector('[data-confirm-cancel]');
  let pendingAction = null;

  const closeModal = () => {
    modal.classList.remove('show');
    modal.setAttribute('aria-hidden', 'true');
    pendingAction = null;
  };

  const openModal = (message, action) => {
    pendingAction = action;
    if (modalMessage) modalMessage.textContent = message || 'Are you sure you want to continue?';
    modal.classList.add('show');
    modal.setAttribute('aria-hidden', 'false');
  };

  document.querySelectorAll('form[data-confirm]').forEach((form) => {
    form.addEventListener('submit', (event) => {
      if (form.dataset.confirmBypass === 'true') {
        form.dataset.confirmBypass = 'false';
        return;
      }
      event.preventDefault();
      openModal(form.dataset.confirm, () => {
        form.dataset.confirmBypass = 'true';
        form.submit();
      });
    });
  });

  document.querySelectorAll('[data-confirm-click]').forEach((el) => {
    el.addEventListener('click', (event) => {
      event.preventDefault();
      openModal(el.getAttribute('data-confirm-click'), () => {
        if (el.tagName === 'A') {
          const href = el.getAttribute('href');
          if (href) window.location.assign(href);
        }
      });
    });
  });

  if (confirmOk) confirmOk.addEventListener('click', () => { if (pendingAction) pendingAction(); closeModal(); });
  if (confirmCancel) confirmCancel.addEventListener('click', closeModal);
  modal.addEventListener('click', (event) => { if (event.target === modal) closeModal(); });
}

function initLoadingPanels() {
  const panels = document.querySelectorAll('[data-loading-panel]');
  if (panels.length === 0) return;

  setTimeout(() => {
    panels.forEach((panel) => {
      panel.hidden = true;
      const dataPanel = panel.parentElement?.querySelector('[data-data-panel]');
      if (dataPanel) dataPanel.hidden = false;
    });
  }, 240);
}

function initFormBusyState() {
  document.querySelectorAll('form').forEach((form) => {
    form.addEventListener('submit', () => {
      const submit = form.querySelector('button[type="submit"]');
      if (!submit || submit.disabled) return;
      submit.classList.add('is-loading');
      submit.disabled = true;
    });
  });
}

function initRowMenuDismiss() {
  document.addEventListener('click', (event) => {
    document.querySelectorAll('.row-menu[open]').forEach((menu) => {
      if (!menu.contains(event.target)) menu.removeAttribute('open');
    });
  });
}

function initRowMenuPlacement() {
  document.querySelectorAll('.row-menu').forEach((menu) => {
    menu.addEventListener('toggle', () => {
      if (!menu.open) {
        menu.classList.remove('row-menu-dropup');
        return;
      }

      const panel = menu.querySelector('.row-menu-list');
      if (!panel) return;

      const menuRect = menu.getBoundingClientRect();
      const panelHeight = panel.offsetHeight || 170;
      const spaceBelow = window.innerHeight - menuRect.bottom;
      const spaceAbove = menuRect.top;
      const shouldOpenUp = spaceBelow < panelHeight + 12 && spaceAbove > spaceBelow;

      menu.classList.toggle('row-menu-dropup', shouldOpenUp);
    });
  });
}

function initTableQuickSearch() {
  document.querySelectorAll('[data-table-search]').forEach((input) => {
    const tableId = input.getAttribute('data-table-target');
    if (!tableId) return;
    const table = document.getElementById(tableId);
    if (!table) return;
    const body = table.querySelector('tbody');
    if (!body) return;
    const rows = Array.from(body.querySelectorAll('tr'));
    const countElId = input.getAttribute('data-table-count-target');
    const countEl = countElId ? document.getElementById(countElId) : null;

    const updateCount = (visible) => {
      if (!countEl) return;
      countEl.textContent = `${visible}/${rows.length}`;
    };

    const applyFilter = () => {
      const q = normalize(input.value);
      let visible = 0;
      rows.forEach((row) => {
        const text = normalize(row.innerText);
        const match = text.includes(q);
        row.style.display = match ? '' : 'none';
        if (match) visible += 1;
      });
      updateCount(visible);
    };

    input.addEventListener('input', applyFilter);
    updateCount(rows.length);
  });
}

function initMobileAnalyticsToggle() {
  const toggle = document.querySelector('[data-mobile-analytics-toggle]');
  const panel = document.querySelector('[data-mobile-analytics-panel]');
  if (!toggle || !panel) return;

  const mobileQuery = window.matchMedia('(max-width: 720px)');

  const syncForViewport = () => {
    const shouldCollapse = mobileQuery.matches;
    panel.classList.toggle('collapsed', shouldCollapse);
    toggle.setAttribute('aria-expanded', shouldCollapse ? 'false' : 'true');
    toggle.textContent = shouldCollapse ? 'Show More Analytics' : 'Hide Extra Analytics';
  };

  syncForViewport();
  mobileQuery.addEventListener('change', syncForViewport);

  toggle.addEventListener('click', () => {
    const collapsed = panel.classList.toggle('collapsed');
    toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
    toggle.textContent = collapsed ? 'Show More Analytics' : 'Hide Extra Analytics';
  });
}

function initDashboardVisuals() {
  document.querySelectorAll('.analytics-line-chart').forEach((svg) => {
    const values = (svg.dataset.points || '')
      .split(',')
      .map((x) => Number(x.trim()))
      .filter((x) => Number.isFinite(x));
    if (values.length < 2) return;

    const width = 600;
    const height = 240;
    const pad = 24;
    const min = Math.min(...values);
    const max = Math.max(...values);
    const span = Math.max(1, max - min);
    const stepX = (width - pad * 2) / (values.length - 1);

    const points = values.map((value, idx) => {
      const x = pad + idx * stepX;
      const ratio = (value - min) / span;
      const y = height - pad - ratio * (height - pad * 2);
      return { x, y };
    });

    const linePath = points
      .map((p, idx) => `${idx === 0 ? 'M' : 'L'} ${p.x.toFixed(2)} ${p.y.toFixed(2)}`)
      .join(' ');

    const areaPath = `M ${points[0].x.toFixed(2)} ${(height - pad).toFixed(2)} ${points
      .map((p) => `L ${p.x.toFixed(2)} ${p.y.toFixed(2)}`)
      .join(' ')} L ${points[points.length - 1].x.toFixed(2)} ${(height - pad).toFixed(2)} Z`;

    const guideLines = [0.25, 0.5, 0.75]
      .map((r) => {
        const y = (pad + (height - pad * 2) * r).toFixed(2);
        return `<line x1="${pad}" y1="${y}" x2="${width - pad}" y2="${y}" stroke="rgba(148,163,184,0.25)" stroke-width="1" />`;
      })
      .join('');

    const pointsMarkup = points
      .map((p) => `<circle cx="${p.x.toFixed(2)}" cy="${p.y.toFixed(2)}" r="3.5" fill="#4361ee" />`)
      .join('');

    svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
    svg.innerHTML = `
      ${guideLines}
      <path d="${areaPath}" fill="rgba(67,97,238,0.18)"></path>
      <path d="${linePath}" fill="none" stroke="#4361ee" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"></path>
      ${pointsMarkup}
    `;
  });

  document.querySelectorAll('.analytics-donut[data-donut-segments]').forEach((el) => {
    const segments = (el.dataset.donutSegments || '')
      .split(',')
      .map((entry) => {
        const [val, color] = entry.split('|');
        return { value: Number(val), color: color || '#94a3b8' };
      })
      .filter((x) => Number.isFinite(x.value) && x.value > 0);

    if (segments.length === 0) return;

    const total = segments.reduce((sum, seg) => sum + seg.value, 0);
    let start = 0;
    const stops = segments.map((seg) => {
      const size = (seg.value / total) * 100;
      const end = start + size;
      const part = `${seg.color} ${start.toFixed(2)}% ${end.toFixed(2)}%`;
      start = end;
      return part;
    });

    el.style.background = `conic-gradient(${stops.join(', ')})`;
  });
}

document.addEventListener('DOMContentLoaded', () => {
  initSidebarToggle();
  initSidebarSearch();
  initDropdowns();
  initThemeCustomizer();
  initTableSorting();
  initDensityToggle();
  initComparisonScreen();
  initPasswordToggle();
  initConfirmModal();
  initLoadingPanels();
  initFormBusyState();
  initRowMenuDismiss();
  initRowMenuPlacement();
  initTableQuickSearch();
  initDashboardVisuals();
  initMobileAnalyticsToggle();
});
