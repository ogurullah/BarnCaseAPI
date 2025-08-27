'use strict';

const $ = (s) => document.querySelector(s);
function msg(text, isError = false) {
  const el = $('#msg');
  el.textContent = text;
  el.style.color = isError ? 'crimson' : 'inherit';
}

// tolerant token extractor in case you later auto-login here
function extractToken(obj) {
  if (!obj || typeof obj !== 'object') return null;
  return obj.jwt || obj.accessToken || obj.access_token || obj.token || null;
}

async function registerUser(creds) {
  const res = await fetch('/auth/register', {
    method: 'POST',
    headers: { 'Accept':'application/json', 'Content-Type':'application/json' },
    body: JSON.stringify(creds)
  });
  const text = await res.text();
  let data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (!res.ok) {
    // surface API message (e.g., "Password must be at least 8 characters")
    const reason = (data && (data.message || data.error || data.title)) || res.statusText || 'Registration failed';
    throw new Error(`${res.status} ${reason}`);
  }
  return data;
}

function nextUrl() {
  // after sign-up, send them to login
  const p = new URLSearchParams(location.search).get('next');
  if (p && /^\/(?!\/)/.test(p)) return p;
  return 'login.html';
}

document.addEventListener('DOMContentLoaded', () => {
  $('#register-form').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const btn = $('#register-btn');
    btn.disabled = true;

    const name = $('#name').value.trim();
    const password = $('#password').value;
    const confirm = $('#confirm').value;

    if (!name || !password || !confirm) {
      msg('All fields are required.', true);
      btn.disabled = false;
      return;
    }
    if (password !== confirm) {
      msg('Passwords do not match.', true);
      btn.disabled = false;
      return;
    }

    try {
      msg('Creating your account…');
      await registerUser({ name, password });
      msg('Account created. Redirecting to sign in…');
      setTimeout(() => { location.href = nextUrl(); }, 250);
    } catch (e) {
      msg(e.message || String(e), true);
    } finally {
      btn.disabled = false;
    }
  });
});
