const grid       = document.getElementById('grid');
const status     = document.getElementById('status');
const input      = document.getElementById('file');
const uploadBtn  = document.getElementById('uploadLabel');

function escapeHtml(s) {
  return s.replace(/[&<>"']/g, c => (
    { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]
  ));
}

async function load() {
  const images = await (await fetch('/api/images')).json();

  if (images.length === 0) {
    grid.innerHTML = `
      <div class="empty">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"
             stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
          <circle cx="8.5" cy="8.5" r="1.5"/>
          <polyline points="21 15 16 10 5 21"/>
        </svg>
        <p>Пока пусто. Загрузите первую картинку.</p>
      </div>`;
    return;
  }

  grid.innerHTML = images.map(img => `
    <a class="card" href="${img.url}" target="_blank" rel="noreferrer">
      <img src="${img.url}" alt="${escapeHtml(img.name)}" loading="lazy">
      <div class="overlay">${escapeHtml(img.name)}</div>
    </a>
  `).join('');
}

input.addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;

  status.textContent = 'Загрузка…';
  status.className = 'busy';
  uploadBtn.style.pointerEvents = 'none';
  uploadBtn.style.opacity = '0.6';

  const fd = new FormData();
  fd.append('file', file);
  const res = await fetch('/api/images', { method: 'POST', body: fd });

  uploadBtn.style.pointerEvents = '';
  uploadBtn.style.opacity = '';

  if (res.ok) {
    status.textContent = '';
    status.className = '';
    await load();
  } else {
    status.textContent = await res.text();
    status.className = 'error';
  }
  input.value = '';
});

load();