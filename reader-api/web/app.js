// Reader API client + SPA. Sem dependências — consome apenas a Reader API.
// A URL da API é configurável por ambiente via window.READER_API_URL ou meta tag.

const API_URL =
  window.READER_API_URL ||
  document.querySelector('meta[name="reader-api-url"]')?.content ||
  window.location.origin; // servido pela própria API como arquivo estático

const PROVIDER = 'MangaDex';
const ZOOM_LEVELS = [1, 1.5, 2];

// ---------- Estado ----------
const state = {
  token: localStorage.getItem('reader.token') || null,
  view: 'auth',
  searchQuery: '',
  detail: null, // { manga, chapters, inLibrary, isFavorite }
  reader: null, // { mangaId, chapter, pages, page, zoom, saveTimer }
};

const $ = (id) => document.getElementById(id);

// ---------- API ----------
async function api(path, { method = 'GET', body, auth = true } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (auth && state.token) headers['Authorization'] = `Bearer ${state.token}`;

  const res = await fetch(`${API_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401) {
    state.token = null;
    localStorage.removeItem('reader.token');
    updateAuthButton();
    throw new Error('Faça login para acessar este recurso.');
  }
  if (res.status === 204) return null;
  if (!res.ok) {
    let message = `Erro ${res.status}`;
    try {
      const problem = await res.json();
      message = problem.detail || problem.title || message;
    } catch { /* mantém mensagem padrão */ }
    throw new Error(message);
  }
  return res.status === 204 ? null : res.json();
}

// ---------- Helpers de UI ----------
function show(view) {
  state.view = view;
  for (const v of ['auth', 'shell', 'detail', 'reader']) {
    $(`view-${v}`).hidden = v !== view;
  }
  $('boot').hidden = true;
}

function setStatus(text) {
  const el = $('content-status');
  el.textContent = text || '';
  el.hidden = !text;
}

function coverCard(item, { onOpen, showFav = false }) {
  const card = document.createElement('button');
  card.className = 'card';
  card.type = 'button';
  const coverUrl = item.coverUrl || '';
  card.innerHTML = `
    ${coverUrl ? `<img class="card-cover" src="${coverUrl}" alt="" loading="lazy" />` : `<div class="card-cover"></div>`}
    <div class="card-body">
      <p class="card-title"></p>
      <p class="card-meta">${item.originalLanguage || ''}${showFav && item.isFavorite ? ' · <span class="card-fav">★</span>' : ''}</p>
    </div>`;
  card.querySelector('.card-title').textContent = item.title;
  card.addEventListener('click', () => onOpen(item));
  return card;
}

// ---------- Auth ----------
async function login(email, password) {
  const data = await api('/api/auth/login', { method: 'POST', auth: false, body: { email, password } });
  state.token = data.accessToken;
  localStorage.setItem('reader.token', state.token);
}

async function register(email, password, displayName) {
  const data = await api('/api/auth/register', { method: 'POST', auth: false, body: { email, password, displayName } });
  state.token = data.accessToken;
  localStorage.setItem('reader.token', state.token);
}

function logout() {
  state.token = null;
  localStorage.removeItem('reader.token');
  state.reader = null;
  updateAuthButton();
  // Volta ao catálogo público ao sair.
  show('shell');
  searchCatalog('');
}

// ---------- Telas do shell ----------
async function showLibrary(favoriteOnly = false) {
  if (!state.token) {
    setStatus('Entre para ver sua biblioteca.');
    $('grid').replaceChildren();
    $('empty').hidden = true;
    return;
  }
  setStatus('Carregando biblioteca…');
  try {
    const result = await api(`/api/library?favoriteOnly=${favoriteOnly}&pageSize=100`);
    setStatus('');
    $('grid').replaceChildren(
      ...result.items.map((item) =>
        coverCard(
          {
            id: item.manga.externalId,
            title: item.manga.title,
            coverUrl: item.manga.coverUrl,
            originalLanguage: item.manga.originalLanguage,
            isFavorite: item.isFavorite,
          },
          { onOpen: (m) => openDetail(m.id), showFav: true },
        ),
      ),
    );
    $('empty').hidden = result.items.length > 0;
  } catch (e) {
    setStatus(e.message);
    $('grid').replaceChildren();
    $('empty').hidden = true;
  }
}

async function showContinueReading() {
  if (!state.token) {
    setStatus('Entre para continuar lendo.');
    $('grid').replaceChildren();
    $('empty').hidden = true;
    return;
  }
  setStatus('Carregando…');
  try {
    const resume = await api('/api/library/continue-reading');
    setStatus('');
    if (!resume) {
      $('grid').replaceChildren();
      $('empty').hidden = false;
      return;
    }
    $('empty').hidden = true;
    $('grid').replaceChildren(
      coverCard(
        {
          id: resume.manga.externalId,
          title: resume.manga.title,
          coverUrl: resume.manga.coverUrl,
          originalLanguage: resume.manga.originalLanguage,
        },
        { onOpen: () => openChapter(resume.manga.externalId, resume.progress) },
      ),
    );
  } catch (e) {
    setStatus('');
    $('grid').replaceChildren();
    $('empty').hidden = false;
  }
}

async function searchCatalog(query) {
  const term = (query || '').trim();
  setStatus(term ? 'Buscando no catálogo…' : 'Carregando catálogo…');
  try {
    const qs = term ? `?title=${encodeURIComponent(term)}&pageSize=30` : '?pageSize=30';
    const result = await api(`/api/catalog/manga${qs}`);
    setStatus('');
    $('grid').replaceChildren(
      ...result.items.map((m) =>
        coverCard(m, { onOpen: (item) => openDetail(item.id) }),
      ),
    );
    $('empty').hidden = result.items.length > 0;
  } catch (e) {
    setStatus(`Catálogo indisponível: ${e.message}`);
    $('grid').replaceChildren();
  }
}

// ---------- Detalhe ----------
async function openDetail(mangaId) {
  show('detail');
  $('detail-title').textContent = 'Carregando…';
  $('detail-desc').textContent = '';
  $('chapter-list').replaceChildren();
  try {
    const [manga, chapters] = await Promise.all([
      api(`/api/catalog/manga/${mangaId}`),
      api(`/api/catalog/manga/${mangaId}/chapters?pageSize=100`),
    ]);

    let inLibrary = false;
    let isFavorite = false;
    if (state.token) {
      try {
        const lib = await api('/api/library?pageSize=100');
        const found = lib.items.find((i) => i.manga.externalId === mangaId);
        if (found) { inLibrary = true; isFavorite = found.isFavorite; }
      } catch { /* ignora */ }
    }

    state.detail = { manga, chapters: chapters.items, inLibrary, isFavorite, mangaId };

    $('detail-cover').src = manga.coverUrl || '';
    $('detail-title').textContent = manga.title;
    $('detail-meta').textContent = [manga.originalLanguage, PROVIDER].filter(Boolean).join(' · ');
    $('detail-desc').textContent = manga.description || 'Sem descrição.';

    const addBtn = $('btn-add-library');
    addBtn.textContent = inLibrary ? 'Na biblioteca ✓' : 'Adicionar à biblioteca';
    addBtn.disabled = inLibrary || !state.token;
    if (!state.token) addBtn.title = 'Entre para adicionar à biblioteca';
    const favBtn = $('btn-toggle-fav');
    favBtn.hidden = !inLibrary;
    favBtn.textContent = isFavorite ? '★ Favorito' : '☆ Favoritar';

    $('chapter-list').replaceChildren(
      ...chapters.items.map((ch) => {
        const li = document.createElement('li');
        li.className = 'chapter-item';
        const label = [
          ch.volume ? `Vol. ${ch.volume}` : null,
          ch.number ? `Cap. ${ch.number}` : null,
          ch.title || null,
        ].filter(Boolean).join(' — ') || ch.id;
        li.innerHTML = `<span class="chapter-name"></span><span class="chapter-sub">${ch.language}</span>`;
        li.querySelector('.chapter-name').textContent = label;
        li.addEventListener('click', () => openChapter(mangaId, null, ch));
        return li;
      }),
    );
    if (chapters.items.length === 0) {
      $('chapter-list').innerHTML = '<li class="chapter-sub">Nenhum capítulo legível disponível.</li>';
    }
  } catch (e) {
    $('detail-title').textContent = 'Erro ao carregar';
    $('detail-desc').textContent = e.message;
  }
}

// ---------- Leitor ----------
async function openChapter(mangaId, progress, chapter) {
  show('reader');
  const chapterId = chapter?.id || progress?.chapter.externalId;
  const stage = $('reader-stage');
  $('reader-page').src = '';
  $('reader-counter').textContent = 'Carregando…';

  try {
    const pages = await api(`/api/catalog/chapters/${chapterId}/pages`);
    const startPage = !chapter && progress ? progress.currentPage - 1 : 0;

    state.reader = {
      mangaId,
      chapterId,
      chapter: chapter || {
        provider: PROVIDER,
        externalId: chapterId,
        language: progress?.chapter.language || 'pt-br',
        title: progress?.chapter.title || null,
        volume: progress?.chapter.volume || null,
        number: progress?.chapter.number || null,
      },
      pages: pages.pageUrls,
      page: Math.max(0, Math.min(startPage, pages.pageUrls.length - 1)),
      zoom: 1,
      saveTimer: null,
    };

    renderPage();
  } catch (e) {
    $('reader-counter').textContent = `Erro: ${e.message}`;
  }
}

function renderPage() {
  const r = state.reader;
  if (!r) return;
  const total = r.pages.length;
  $('reader-page').src = r.pages[r.page];
  $('reader-counter').textContent = `${r.page + 1} / ${total}`;
  $('reader-progress').querySelector('span').style.width = `${((r.page + 1) / total) * 100}%`;
  $('reader-stage').dataset.zoom = String(r.zoom);
  scheduleProgressSave();
}

function scheduleProgressSave() {
  const r = state.reader;
  if (!r) return;
  clearTimeout(r.saveTimer);
  r.saveTimer = setTimeout(() => saveProgress(false), 800);
}

async function saveProgress(markComplete) {
  const r = state.reader;
  if (!r) return;
  try {
    await api(`/api/library/${r.mangaId}/progress`, {
      method: 'PUT',
      body: {
        chapter: r.chapter,
        currentPage: r.page + 1,
        pageCount: r.pages.length,
      },
    });
    if (markComplete) {
      await api(`/api/library/${r.mangaId}/chapters/${r.chapterId}/complete`, { method: 'POST' });
    }
  } catch { /* progresso é best-effort */ }
}

function nextPage() {
  const r = state.reader;
  if (r && r.page < r.pages.length - 1) { r.page++; renderPage(); }
}
function prevPage() {
  const r = state.reader;
  if (r && r.page > 0) { r.page--; renderPage(); }
}
function cycleZoom() {
  const r = state.reader;
  if (!r) return;
  r.zoom = ZOOM_LEVELS[(ZOOM_LEVELS.indexOf(r.zoom) + 1) % ZOOM_LEVELS.length];
  renderPage();
}

async function closeReader() {
  const r = state.reader;
  if (r) { clearTimeout(r.saveTimer); await saveProgress(false); }
  state.reader = null;
  show('shell');
  await searchCatalog('');
}

// ---------- Eventos ----------
function bindEvents() {
  // Auth tabs
  $('tab-login').addEventListener('click', () => switchAuthTab(true));
  $('tab-register').addEventListener('click', () => switchAuthTab(false));

  $('form-login').addEventListener('submit', async (e) => {
    e.preventDefault();
    authError('');
    const form = new FormData(e.target);
    try {
      await login(form.get('email'), form.get('password'));
      e.target.reset();
      enterApp();
    } catch (err) { authError(err.message); }
  });

  $('form-register').addEventListener('submit', async (e) => {
    e.preventDefault();
    authError('');
    const form = new FormData(e.target);
    try {
      await register(form.get('email'), form.get('password'), form.get('displayName'));
      e.target.reset();
      enterApp();
    } catch (err) { authError(err.message); }
  });

  $('btn-auth').addEventListener('click', () => {
    if (state.token) logout();
    else show('auth');
  });

  // Nav
  document.querySelectorAll('.nav-btn[data-view]').forEach((btn) => {
    btn.addEventListener('click', async () => {
      document.querySelectorAll('.nav-btn[data-view]').forEach((b) => b.classList.remove('is-active'));
      btn.classList.add('is-active');
      $('search-input').value = '';
      state.searchQuery = '';
      if (btn.dataset.view === 'catalog') await searchCatalog('');
      else if (btn.dataset.view === 'library') await showLibrary();
      else if (btn.dataset.view === 'continue') await showContinueReading();
    });
  });

  // Busca com debounce
  let searchTimer;
  $('search-input').addEventListener('input', (e) => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => searchCatalog(e.target.value), 400);
  });

  // Detail
  $('btn-detail-back').addEventListener('click', async () => { show('shell'); await searchCatalog(''); });
  $('btn-add-library').addEventListener('click', async () => {
    const d = state.detail;
    if (!d) return;
    try {
      await api('/api/library', {
        method: 'POST',
        body: {
          manga: {
            provider: PROVIDER,
            externalId: d.manga.id,
            title: d.manga.title,
            coverUrl: d.manga.coverUrl,
            originalLanguage: d.manga.originalLanguage,
          },
          isFavorite: false,
        },
      });
      await openDetail(d.mangaId);
    } catch (e) { alert(e.message); }
  });
  $('btn-toggle-fav').addEventListener('click', async () => {
    const d = state.detail;
    if (!d) return;
    try {
      await api(`/api/library/${d.mangaId}/favorite`, { method: 'PUT', body: { isFavorite: !d.isFavorite } });
      await openDetail(d.mangaId);
    } catch (e) { alert(e.message); }
  });

  // Reader
  $('reader-next').addEventListener('click', nextPage);
  $('reader-prev').addEventListener('click', prevPage);
  $('reader-zoom').addEventListener('click', cycleZoom);
  $('reader-back').addEventListener('click', closeReader);
  $('reader-complete').addEventListener('click', async () => { await saveProgress(true); await closeReader(); });

  document.addEventListener('keydown', (e) => {
    if (state.view !== 'reader') return;
    if (e.key === 'ArrowRight' || e.key === 'ArrowDown') nextPage();
    if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') prevPage();
    if (e.key === 'Escape') closeReader();
    if (e.key === 'z' || e.key === 'Z') cycleZoom();
  });

  // Salva progresso ao sair/pausar a página
  window.addEventListener('beforeunload', () => {
    const r = state.reader;
    if (r && navigator.sendBeacon) {
      // sendBeacon não aceita header Authorization; usamos fetch keepalive como melhor esforço.
      clearTimeout(r.saveTimer);
      saveProgress(false);
    }
  });
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden' && state.reader) {
      clearTimeout(state.reader.saveTimer);
      saveProgress(false);
    }
  });
}

function switchAuthTab(isLogin) {
  $('tab-login').classList.toggle('is-active', isLogin);
  $('tab-register').classList.toggle('is-active', !isLogin);
  $('form-login').hidden = !isLogin;
  $('form-register').hidden = isLogin;
  authError('');
}

function updateAuthButton() {
  const btn = $('btn-auth');
  if (btn) btn.textContent = state.token ? 'Sair' : 'Entrar';
}

function authError(message) {
  const el = $('auth-error');
  el.textContent = message;
  el.hidden = !message;
}

async function enterApp() {
  show('shell');
  updateAuthButton();
  await searchCatalog('');
}

// ---------- Boot ----------
(async function boot() {
  bindEvents();
  if (state.token) {
    try {
      await api('/api/users/me');
      await enterApp();
      return;
    } catch {
      state.token = null;
      localStorage.removeItem('reader.token');
    }
  }
  updateAuthButton();
  // Catálogo é público: mostra mesmo sem login.
  await enterApp();
})();
