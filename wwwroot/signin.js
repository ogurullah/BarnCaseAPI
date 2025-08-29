'use strict';

// tiny helpers
const $ = (s) => document.querySelector(s);

function msg(text, isError = false) {
  const el = document.getElementById('msg');
  if (!el) {
    return;
  }
  el.textContent = text;
  el.style.color = isError ? 'crimson' : 'inherit';
}

function extractToken(obj) {
  if (!obj || typeof obj !== 'object') {
    return null;
  }
  if (obj.jwt) return obj.jwt;
  if (obj.accessToken) return obj.accessToken;
  if (obj.access_token) return obj.access_token;
  if (obj.token) return obj.token;
  if (obj.AccessToken) return obj.AccessToken;
  return null;
}

function storeTokenSession(token) {
  sessionStorage.setItem('authToken', token);
}

function nextUrl() {
  const params = new URLSearchParams(location.search);
  const candidate = params.get('next');

  // only allow same-origin relative paths
  if (candidate && /^\/(?!\/)/.test(candidate)) {
    return candidate;
  }
  return 'app.html';
}

async function login(credentials) {
  const res = await fetch('/auth/login', {
    method: 'POST',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(credentials)
  });

  const raw = await res.text();

  let data = null;
  try {
    data = raw ? JSON.parse(raw) : null;
  } catch {
    data = raw;
  }

  if (!res.ok) {
    const reason =
      (data && (data.message || data.error || data.title)) ||
      res.statusText ||
      'Login failed';
    throw new Error(res.status + ' ' + reason);
  }

  const token = extractToken(data);
  if (!token) {
    throw new Error('Login succeeded but no token found in response.');
  }

  return token;
}

document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  if (!form) {
    return;
  }

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();

    const btn = document.getElementById('login-btn');
    if (btn) {
      btn.disabled = true;
    }

    msg('Signing in…');

    const nameEl = document.getElementById('name');
    const passEl = document.getElementById('password');

    const name = (nameEl?.value || '').trim();
    const password = passEl?.value || '';

    if (!name || !password) {
      msg('Username and password are required.', true);
      if (btn) btn.disabled = false;
      return;
    }

    try {
      const token = await login({ name, password });
      storeTokenSession(token);
      msg('Success. Redirecting…');

      setTimeout(() => {
        location.href = nextUrl();
      }, 100);
    } catch (err) {
      msg(err?.message ? String(err.message) : String(err), true);
    } finally {
      if (btn) {
        btn.disabled = false;
      }
    }
  });
});
