function refreshPdf() {
  const frame = document.querySelector('.pdf-frame');
  if (frame) frame.src = frame.src;
}

function normalize(value) {
  return (value || '').trim().toLowerCase();
}

function initComparisonScreen() {
  const layoutSelect = document.getElementById('layoutSelect');
  const layout = document.getElementById('comparisonLayout');
  const rows = document.querySelectorAll('.comparison-row');
  const mismatchCount = document.getElementById('mismatchCount');
  const submitBtn = document.getElementById('submitBtn');

  if (layoutSelect && layout) {
    layoutSelect.addEventListener('change', () => {
      layout.className = `comparison-layout ${layoutSelect.value}`;
    });
  }

  const evaluate = () => {
    let mismatches = 0;
    rows.forEach((row) => {
      const expected = normalize(row.dataset.expected);
      const input = row.querySelector('input[name$="ActualValue"]');
      const actual = normalize(input ? input.value : '');
      const isBlocking = row.dataset.blocking === 'true';
      const indicator = row.querySelector('.match-indicator');
      const match = expected === actual;
      indicator.textContent = match ? 'Match' : 'Mismatch';
      indicator.className = `match-indicator ${match ? 'ok' : 'bad'}`;
      if (!match && isBlocking) mismatches++;
    });

    mismatchCount.textContent = String(mismatches);
    if (submitBtn) submitBtn.disabled = mismatches > 0;
  };

  rows.forEach((row) => {
    const input = row.querySelector('input[name$="ActualValue"]');
    if (input) input.addEventListener('input', evaluate);
  });
  evaluate();
}

document.querySelectorAll('form[data-confirm]').forEach((form) => {
  form.addEventListener('submit', (event) => {
    if (!confirm(form.dataset.confirm || 'Are you sure?')) event.preventDefault();
  });
});
