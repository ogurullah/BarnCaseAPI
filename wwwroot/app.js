'use strict';

// config
const cfg = {
  whoami: '/auth/whoami',
  refresh: '/auth/refresh',
  farms: '/api/farms',
  farmsMine: '/api/farms/mine'
};

const $ = s => document.querySelector(s);
const jsonOut = $('#json-out');
const tableOut = $('#table-out');

// simple HTML escaper to render names safely
function esc(s) {
  return String(s ?? '').replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));
}

// state for future filtering
const farmsState = { list: [], selectedId: null };

// balance helpers
function toNumberOrNull(v) {
  if (v === null || v === undefined) return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}
function setBalanceChip(val) {
  const el = document.getElementById('who-balance');
  if (!el) return;
  if (val === null || val === undefined) { el.textContent = '—'; return; }
  // Locale-friendly number. No currency symbol (unknown).
  el.textContent = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(val);
}

// jwt token functions
// reading, refreshing and saving tokens
function getToken() { return sessionStorage.getItem('authToken'); }
function decodeJwt(token) {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g,'+').replace(/_/g,'/'));
    return JSON.parse(json);
  } catch { return null; }
}
function tokenExpired(payload) {
  if (!payload || !payload.exp) return false;
  return (Date.now() / 1000) >= payload.exp;
}
function extractToken(x) {
  if (!x) return null;
  if (typeof x === 'string') return x.split('.').length === 3 ? x : null;
  if (typeof x === 'object') return x.jwt || x.accessToken || x.access_token || x.token || x.AccessToken || null;
  return null;
}
function secondsUntilExpiry(p) {
  return p?.exp ? (p.exp - Math.floor(Date.now()/1000)) : Number.POSITIVE_INFINITY;
}

const REFRESH_SKEW = 60;
let refreshTimer = null;
let refreshingPromise = null;

function saveToken(token) {
  sessionStorage.setItem('authToken', token);
  reflectUserFromToken(token);
  scheduleRefresh();
}

function scheduleRefresh() {
  clearTimeout(refreshTimer);
  const p = decodeJwt(getToken());
  const sec = p?.exp ? (p.exp - Math.floor(Date.now()/1000)) - REFRESH_SKEW : Infinity;
  if (isFinite(sec) && sec > 0) {
    refreshTimer = setTimeout(() => { ensureFreshToken().catch(()=>{}); }, sec * 1000);
  }
}

// auto refresh system
async function doRefresh() {
  const token = getToken();
  const h = { 'Accept':'application/json' };
  if (token) h['Authorization'] = `Bearer ${token}`;
  const res = await fetch(cfg.refresh, { method:'POST', headers: h });

  const text = await res.text();
  let data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) return null;

  const bodyToken = (data && (data.jwt || data.accessToken || data.access_token || data.token || data.AccessToken)) || null;
  if (bodyToken) return bodyToken;
  const auth = res.headers.get('Authorization');
  if (auth && auth.startsWith('Bearer ')) return auth.slice(7);
  return null;
}

async function ensureFreshToken() {
  if (refreshingPromise) return refreshingPromise;
  refreshingPromise = (async () => {
    const t = getToken();
    if (!t) return false;
    const p = decodeJwt(t);
    const secondsLeft = p?.exp ? (p.exp - Math.floor(Date.now()/1000)) : Infinity;
    if (secondsLeft > REFRESH_SKEW) return true;
    const newToken = await doRefresh();
    if (!newToken) return false;
    saveToken(newToken);
    return true;
  })();
  try { return await refreshingPromise; }
  finally { refreshingPromise = null; }
}

// showing roles and authentication on the page
function normalizeRolesFromJwt(p) {
  const keys = ['roles', 'role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const out = [];
  for (const k of keys) {
    const v = p?.[k];
    if (Array.isArray(v)) out.push(...v);
    else if (typeof v === 'string') out.push(v);
  }
  return Array.from(new Set(out));
}

function setRolesChip(roles) {
  const el = document.getElementById('who-roles');
  if (!el) return;
  const list = Array.isArray(roles) ? roles : (roles ? [roles] : []);
  el.textContent = list.length ? list.join(', ') : '—';
}

function setAuthStatus(state) {
  const el = document.getElementById('auth-status');
  if (!el) return;
  el.dataset.state = state;
  el.textContent =
    state === 'ok' ? 'Authorized' :
    state === 'checking' ? 'Checking…' :
    'Unauthorized';
}

// run for getting user session data and authorization info
async function runWhoAmIOnce() {
  setAuthStatus('checking');
  try {
    const r = await api(cfg.whoami);
    const ok = (r?.body && typeof r.body === 'object' && 'isAuthenticated' in r.body)
      ? !!r.body.isAuthenticated
      : r?.status === 200;
    setAuthStatus(ok ? 'ok' : 'bad');
    showJson(r.body); // can remove after developing finishes
    if (r?.body?.roles) {
    setRolesChip(r.body.roles);
    } else if (Array.isArray(r?.body?.claims)) {
    const uri = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    setRolesChip(r.body.claims.filter(c => c.Type === 'role' || c.Type === uri).map(c => c.Value));
    }
    // try to read a balance from whoami; support a few likely shapes
    // r.body.balance, r.body.accountBalance, r.body.wallet.balance, r.body.credits, etc.
    let bal = null;
    const b = r?.body ?? {};
    const candidates = [
      b.balance, b.accountBalance, b.Balance, b.AccountBalance, b.credits, b.Credits,
      b.wallet?.balance, b.Wallet?.Balance, b.profile?.balance, b.user?.balance
    ];
    for (const c of candidates) {
      const n = toNumberOrNull(c);
      if (n !== null) { bal = n; break; }
    }
    setBalanceChip(bal);
  } catch (e) {
    setAuthStatus('bad');
    showJson(e);
  }
}

// api wrapper
async function api(path, { method='GET', body=null, headers={} } = {}) {
  const token = getToken();
  const h = { 'Accept':'application/json', ...headers };
  if (body !== null && typeof body !== 'string') {
    h['Content-Type'] = 'application/json';
    body = JSON.stringify(body);
  }
  if (token) h['Authorization'] = `Bearer ${token}`;

  let res = await fetch(path, { method, headers: h, body });

  let text = await res.text();
  let data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (res.status === 401) {
    const refreshed = await ensureFreshToken();
    if (refreshed) {
      const token2 = getToken();
      if (token2) h['Authorization'] = `Bearer ${token2}`;
      res = await fetch(path, { method, headers: h, body });
      text = await res.text();
      data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
    }
  }
  if (res.status === 401) {
    sessionStorage.removeItem('authToken');
    location.href = `signin.html?next=${encodeURIComponent(location.pathname)}`;
    return;
  }
  if (!res.ok) throw { status: res.status, statusText: res.statusText, body: data };
  return { status: res.status, headers: res.headers, body: data };
}

// dev renderers, can be removed later
function showJson(x) {
  tableOut.innerHTML = '';
  jsonOut.textContent = typeof x === 'string' ? x : JSON.stringify(x, null, 2);
}
function renderTable(arr) {
  if (!Array.isArray(arr) || arr.length === 0 || typeof arr[0] !== 'object') { showJson(arr); return; }
  const cols = Array.from(new Set(arr.flatMap(o => Object.keys(o))));
  const escape = (v) => (v === null || v === undefined) ? '' : String(v);
  const thead = `<thead><tr>${cols.map(c=>`<th>${c}</th>`).join('')}</tr></thead>`;
  const rows = arr.map(o => `<tr>${cols.map(c=>`<td>${escape(o[c])}</td>`).join('')}</tr>`).join('');
  tableOut.innerHTML = `<div style="max-height:50vh;overflow:auto"><table>${thead}<tbody>${rows}</tbody></table></div>`;
  jsonOut.textContent = JSON.stringify(arr, null, 2);
}

// --- My Farms box logic ---
function renderFarms() {
  const host = document.getElementById('farms-list');
  if (!host) return;
  const farms = Array.isArray(farmsState.list) ? farmsState.list : [];
  if (farms.length === 0) {
    host.innerHTML = `<span class="chip">No farms found.</span>`;
    return;
  }
  const html = farms.map(f => {
    const id = f.id ?? f.Id ?? f.farmId ?? f.FarmId;
    const name = f.name ?? f.Name ?? `Farm #${id ?? '—'}`;
    const selected = String(farmsState.selectedId ?? '') === String(id ?? '');
    const pressed = selected ? 'true' : 'false';
    const check = selected ? '✓ ' : '';
    return `<button class="btn" data-farmid="${esc(id)}" ${selected ? 'data-selected="true"' : ''} aria-pressed="${pressed}" title="${selected ? 'Selected – click to deselect' : 'Click to select'}">${check}${esc(name)}</button>`;
  }).join('');
  host.innerHTML = html;
}

async function loadMyFarms() {
  const host = document.getElementById('farms-list');
  if (host) host.innerHTML = `<span class="chip">Loading…</span>`;
  try {
    const r = await api(cfg.farmsMine);
    farmsState.list = Array.isArray(r?.body) ? r.body : [];
    // preserve selection if it still exists
    if (!farmsState.list.some(f => String(f.id ?? f.Id) === String(farmsState.selectedId))) {
      farmsState.selectedId = null;
    }
    renderFarms();
  } catch (e) {
    if (host) host.innerHTML = `<span class="chip">Failed to load.</span>`;
    showJson(e);
  }
}


// updates info on header from jwt token
function reflectUserFromToken(token) {
  const p = decodeJwt(token);
  $('#who-name').textContent = p?.name || p?.unique_name || p?.sub || '—';
  const roles = normalizeRolesFromJwt(p);
  setRolesChip(roles);
//  $('#who-exp').textContent = 'exp: ' + (p?.exp ? new Date(p.exp * 1000).toLocaleString() : 'n/a');
}

// boot
async function boot() {
  const token = getToken();
  if (!token) {
    location.href = `signin.html?next=${encodeURIComponent(location.pathname)}`;
    return;
  }
  const p = decodeJwt(token);
  if (tokenExpired(p)) {
    sessionStorage.removeItem('authToken');
    location.href = `signin.html?next=${encodeURIComponent(location.pathname)}`;
    return;
  }
  reflectUserFromToken(token);

  try {
    const r = await api(cfg.whoami);
    if (r?.body) {
      $('#who-name').textContent = r.body.name || $('#who-name').textContent;
      showJson(r.body);
    }
    await loadMyFarms();
  } catch (e) {
    showJson(e);
  }
}

document.addEventListener('DOMContentLoaded', () => {
  $('#logout-btn').addEventListener('click', () => {
    sessionStorage.removeItem('authToken');
    location.href = 'signin.html';
  });

  $('#btn-whoami').addEventListener('click', async () => {
    try { const r = await api(cfg.whoami); showJson(r.body); }
    catch (e) { showJson(e); }
  });

  $('#btn-farms').addEventListener('click', async () => {
    try { const r = await api(cfg.farms); renderTable(r.body); }
    catch (e) { showJson(e); }
  });

  // refresh farms on demand
  const refreshBtn = document.getElementById('btn-refresh-farms');
  if (refreshBtn) refreshBtn.addEventListener('click', loadMyFarms);

  // create farm flow
  const createBtn = document.getElementById('btn-create-farm');
  if (createBtn) createBtn.addEventListener('click', async () => {
    const name = (prompt('New farm name:') || '').trim();
    if (!name) return; // user cancelled or empty
    createBtn.disabled = true;
    try {
      const r = await api(cfg.farms, {
        method: 'POST',
        body: { farmName: name }
      });
      // Try to pick the new farm’s id/name from the response
      const created = r?.body || {};
      const newId = created.id ?? created.Id ?? created.farmId ?? created.FarmId ?? null;
      farmsState.selectedId = newId;
      await loadMyFarms();
    } catch (e) {
      showJson(e);
      alert('Failed to create farm.');
    } finally {
      createBtn.disabled = false;
    }
  });

  // delete farm flow (selected only)
  const deleteBtn = document.getElementById('btn-delete-farm');
  if (deleteBtn) deleteBtn.addEventListener('click', async () => {
    const id = farmsState.selectedId;
    if (!id) { alert('Select a farm to delete.'); return; }
    const name = (farmsState.list.find(f => String(f.id ?? f.Id) === String(id))?.name) ?? `#${id}`;
    if (!confirm(`Are you sure you want to delete farm "${name}"? This cannot be undone.`)) return;
    deleteBtn.disabled = true;
    try {
      await api(`${cfg.farms}/${encodeURIComponent(id)}`, { method: 'DELETE' });
      // clear selection and refresh list
      farmsState.selectedId = null;
      await loadMyFarms();
    } catch (e) {
      showJson(e);
      alert('Failed to delete farm.');
    } finally {
      deleteBtn.disabled = false;
    }
  });
  
  // click-to-toggle select
  const farmsList = document.getElementById('farms-list');
  if (farmsList) farmsList.addEventListener('click', (e) => {
    // clicks can originate from text nodes; normalize to an Element
    let node = e.target;
    if (!(node instanceof Element)) node = node.parentElement;
    if (!node) return;
    const btn = node.closest('button[data-farmid]');
    if (!btn) return;
    const id = btn.getAttribute('data-farmid');
    farmsState.selectedId = (String(farmsState.selectedId) === String(id)) ? null : id;
    renderFarms();
  });

  // keep it to a single pass:
  boot();
  runWhoAmIOnce();
  scheduleRefresh();
});