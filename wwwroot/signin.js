'use strict';

// tiny helpers
const $ = (s) => document.querySelector(s);
function msg(text, isError = false) {
  const el = $('#msg');
  el.textContent = text;
  el.style.color = isError ? 'crimson' : 'inherit';
}

function extractToken(obj) {
  if (!obj || typeof obj !== 'object') return null;
  return obj.jwt || obj.accessToken || obj.access_token || obj.token || obj.AccessToken || null;
}

function storeTokenSession(token) {
  sessionStorage.setItem('authToken', token);
}

function nextUrl() {
  const p = new URLSearchParams(location.search).get('next');
  // only allow same-origin relative paths
  if (p && /^\/(?!\/)/.test(p)) return p;
  return 'index.html';
}

async function login(creds) {
  const res = await fetch('/auth/login', {
    method: 'POST',
    headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify(creds)
  });

  const text = await res.text();
  let data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (!res.ok) {
    const reason = (data && (data.message || data.error || data.title)) || res.statusText || 'Login failed';
    throw new Error(`${res.status} ${reason}`);
  }
  const token = extractToken(data);
  if (!token) throw new Error('Login succeeded but no token found in response.');
  return token;
}

document.addEventListener('DOMContentLoaded', () => {
  $('#login-form').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const btn = $('#login-btn');
    btn.disabled = true;
    msg('Signing in…');

    const name = $('#name').value.trim();
    const password = $('#password').value;

    if (!name || !password) {
      msg('Username and password are required.', true);
      btn.disabled = false;
      return;
    }

    try {
      const token = await login({ name, password });
      storeTokenSession(token);
      msg('Success. Redirecting…');
      setTimeout(() => { location.href = nextUrl(); }, 100);
    } catch (e) {
      msg(e.message || String(e), true);
    } finally {
      btn.disabled = false;
    }
  });
});
