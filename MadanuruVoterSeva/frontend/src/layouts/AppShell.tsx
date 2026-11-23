import { useEffect, useState, type ReactNode } from 'react';
import { Button, Drawer, IconButton } from '@mui/material';
import {
  AccountBalanceOutlined, ChevronRightRounded, DashboardOutlined,
  LogoutRounded, MenuRounded, SearchRounded, SystemUpdateAltRounded,
} from '@mui/icons-material';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { api } from '../api/client';
import './shell.css';

const navigation = [
  { label: 'Voter lookup', path: '/', icon: SearchRounded },
  { label: 'Dashboard', path: '/dashboard', icon: DashboardOutlined },
  { label: 'Ward directory', path: '/wards', icon: AccountBalanceOutlined },
  { label: 'Roll import', path: '/admin/import', icon: SystemUpdateAltRounded },
];

interface AppShellProps {
  children: ReactNode;
  onSignOut: () => void;
}

export function AppShell({ children, onSignOut }: AppShellProps) {
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const [summary, setSummary] = useState<{ electors: number; wards: number }>();
  const current = navigation.find(item => item.path === location.pathname);

  useEffect(() => {
    let cancelled = false;
    api.getStatistics().then(data => {
      if (!cancelled) setSummary({ electors: data.totalImportedVoters, wards: data.totalWards });
    }).catch(() => {
      if (!cancelled) setSummary(undefined);
    });
    return () => { cancelled = true; };
  }, [location.pathname]);

  function renderSidebar() {
    const close = () => setMenuOpen(false);
    const activeWard = location.pathname === '/wards' ? new URLSearchParams(location.search).get('ward') : null;
    return (
      <div className="desk-sidebar">
        <NavLink className="desk-brand" to="/" onClick={close}>
          <img className="desk-brand-icon" src="/madanuru-mark.svg" width={40} height={40} alt="" />
          <span><strong>Madanuru Seva</strong><small>Electoral Roll 2026</small></span>
        </NavLink>
        <nav className="desk-nav" aria-label="Main navigation">
          {navigation.map(({ label, path, icon: Icon }) => (
            <NavLink key={path} end to={path} onClick={close}>
              <Icon className="desk-nav-icon" aria-hidden="true" />
              <span>{label}</span>
              <ChevronRightRounded className="desk-nav-chevron" aria-hidden="true" />
            </NavLink>
          ))}
        </nav>
        {!!summary?.wards && (
          <section className="desk-wards" aria-label="Ward shortcuts">
            <h2>Jump to ward</h2>
            <div className="desk-ward-grid">
              {Array.from({ length: summary.wards }, (_, i) => i + 1).map(ward => (
                <Link key={ward} to={`/wards?ward=${ward}`} className="desk-ward"
                  aria-label={`Ward ${ward}`} aria-current={activeWard === String(ward) ? 'true' : false}
                  onClick={close}>{ward}</Link>
              ))}
            </div>
          </section>
        )}
        <div className="desk-roll-summary">
          <span className="desk-roll-dot" data-active={!!summary?.electors} />
          <div>
            <strong>{summary?.electors ? `${summary.electors.toLocaleString('en-IN')} electors` : 'Roll status unavailable'}</strong>
            {!!summary?.wards && <small>{summary.wards} wards in the active roll</small>}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="desk-shell">
      <aside className="desk-sidebar-desktop">{renderSidebar()}</aside>
      <Drawer open={menuOpen} onClose={() => setMenuOpen(false)} slotProps={{ paper: { className: 'desk-drawer' } }}>
        {renderSidebar()}
      </Drawer>
      <div className="desk-main">
        <header className="desk-header">
          <div className="desk-header-context">
            <IconButton className="desk-menu-button" aria-label="Open navigation" aria-expanded={menuOpen}
              onClick={() => setMenuOpen(true)}><MenuRounded /></IconButton>
            <strong>{current?.label ?? 'Madanuru Seva'}</strong>
            <span>Madanuru Gram Panchayat</span>
          </div>
          <Button size="small" onClick={onSignOut} startIcon={<LogoutRounded />}>Sign out</Button>
        </header>
        <main className="desk-content">{children}</main>
      </div>
    </div>
  );
}

