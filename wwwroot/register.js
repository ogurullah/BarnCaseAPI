'use strict';

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
  // couldn't remember the exact token name
  if (obj.jwt) return obj.jwt;
  if (obj.accessToken) return obj.accessToken;
  if (obj.access_token) return obj.access_token;
  if (obj.token) return obj.token;
  return null;
}

async function registerUser(credentials) {
  const res = await fetch('/auth/register', {
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
      'Registration failed';
    throw new Error(res.status + ' ' + reason);
  }

  return data;
}

function nextUrl() {
  const params = new URLSearchParams(location.search);
  const candidate = params.get('next');

  // only allow same relative paths like /app.html
  if (candidate && /^\/(?!\/)/.test(candidate)) {
    return candidate;
  }

  return 'signin.html';
}

document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('register-form');
  if (!form) {
    return;
  }

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();

    const btn = document.getElementById('register-btn');
    if (btn) {
      btn.disabled = true;
    }

    const nameEl = document.getElementById('name');
    const passwordEl = document.getElementById('password');
    const confirmEl = document.getElementById('confirm');

    const name = (nameEl?.value || '').trim();
    const password = passwordEl?.value || '';
    const confirm = confirmEl?.value || '';

    if (!name || !password || !confirm) {
      msg('All fields are required.', true);
      if (btn) btn.disabled = false;
      return;
    }

    if (password !== confirm) {
      msg('Passwords do not match.', true);
      if (btn) btn.disabled = false;
      return;
    }

    try {
      msg('Creating your account…');
      await registerUser({ name, password });
      msg('Account created. Redirecting to sign in…');
      setTimeout(() => {
        location.href = nextUrl();
      }, 250);
    } catch (err) {
      msg(err?.message ? String(err.message) : String(err), true);
    } finally {
      if (btn) {
        btn.disabled = false;
      }
    }
  });
});
