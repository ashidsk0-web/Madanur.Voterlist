import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Alert, Button, CircularProgress, IconButton, InputAdornment, TextField } from '@mui/material';
import { LockOutlined, LoginRounded, PersonOutlined, VisibilityOffOutlined, VisibilityOutlined } from '@mui/icons-material';
import axios from 'axios';
import { api } from '../api/client';
import './desk-login.css';

export function DeskSession({ children }: { children: (signOut: () => void) => ReactNode }) {
  const [session, setSession] = useState(() => api.getCurrentUser());
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    const expired = () => {
      setSession(api.getCurrentUser());
      setShowPassword(false);
      setError('Your session expired. Sign in again.');
    };
    window.addEventListener('madanuru-session-expired', expired);
    return () => window.removeEventListener('madanuru-session-expired', expired);
  }, []);

  async function signIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const fields = new FormData(event.currentTarget);
    setBusy(true);
    setError('');
    try {
      await api.login({ username: String(fields.get('username')), password: String(fields.get('password')) });
      setSession(api.getCurrentUser());
      setShowPassword(false);
    } catch (failure) {
      const status = axios.isAxiosError(failure) ? failure.response?.status : undefined;
      if (status === 401) setError('The username or password is incorrect.');
      else if (status === 429) setError('Too many attempts. Wait a minute and try again.');
      else setError('Could not connect to the voter desk. Try again.');
    } finally {
      setBusy(false);
    }
  }

  async function signOut() {
    try {
      await api.logout();
      setSession(api.getCurrentUser());
    } catch {
      setError('Sign-out failed. Check the local API connection.');
    }
  }

  return session.isAuthenticated ? (
    <>
      {error && <Alert severity="error">{error}</Alert>}
      {children(() => void signOut())}
    </>
  ) : (
    <main className="desk-login">
      <div className="desk-login-card">
        <header className="desk-login-brand">
          <img src="/madanuru-mark.svg" width={64} height={64} alt="" />
          <div>
            <p className="desk-login-location">Madanuru Gram Panchayat</p>
            <p className="desk-login-name">Madanuru<br className="desk-login-name-break" /> Voter Seva</p>
            <p className="desk-login-telugu" lang="te">మడనూరు ఓటరు సేవ</p>
          </div>
        </header>
        <section className="desk-login-panel" aria-labelledby="desk-login-title">
          <h1 id="desk-login-title">Sign in</h1>
          <p>Use your administrator account to access the voter desk.</p>
          <form className="desk-login-form" onSubmit={signIn} aria-busy={busy}>
            <TextField fullWidth label="Username" name="username" autoComplete="username" required
              slotProps={{
                htmlInput: { autoCapitalize: 'none', spellCheck: false, readOnly: busy },
                input: { startAdornment: <InputAdornment position="start"><PersonOutlined aria-hidden="true" /></InputAdornment> },
              }} />
            <TextField fullWidth label="Password" name="password" type={showPassword ? 'text' : 'password'}
              autoComplete="current-password" required slotProps={{
                htmlInput: { readOnly: busy },
                input: {
                  startAdornment: <InputAdornment position="start"><LockOutlined aria-hidden="true" /></InputAdornment>,
                  endAdornment: <InputAdornment position="end">
                    <IconButton type="button" edge="end" onClick={() => setShowPassword(visible => !visible)}
                      aria-label={showPassword ? 'Hide password' : 'Show password'}>
                      {showPassword ? <VisibilityOffOutlined /> : <VisibilityOutlined />}
                    </IconButton>
                  </InputAdornment>,
                },
              }} />
            {error && <Alert severity="error">{error}</Alert>}
            <Button type="submit" variant="contained" disabled={busy} endIcon={busy
              ? <CircularProgress size={18} color="inherit" />
              : <LoginRounded />}>
              {busy ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </section>
      </div>
    </main>
  );
}
