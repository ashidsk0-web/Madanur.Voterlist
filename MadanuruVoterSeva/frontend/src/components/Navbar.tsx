import React, { useState, useEffect } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  Button,
  Chip,
  Container,
} from '@mui/material';
import {
  Search as SearchIcon,
  Dashboard as DashboardIcon,
  AccountBalance as WardIcon,
  SystemUpdateAlt as ImportIcon,
  VerifiedUser as ShieldIcon,
  FiberManualRecord as DotIcon,
  AccessTime as ClockIcon,
} from '@mui/icons-material';
import { Link as RouterLink, useLocation } from 'react-router-dom';

export const Navbar: React.FC = () => {
  const location = useLocation();
  const [timeStr, setTimeStr] = useState<string>('');

  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setTimeStr(
        now.toLocaleTimeString('en-IN', {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          hour12: true,
        })
      );
    };
    updateTime();
    const timer = setInterval(updateTime, 1000);
    return () => clearInterval(timer);
  }, []);

  const navItems = [
    { label: 'Voter Search', path: '/', icon: <SearchIcon fontSize="small" /> },
    { label: 'Dashboard', path: '/dashboard', icon: <DashboardIcon fontSize="small" /> },
    { label: 'Ward Directory', path: '/wards', icon: <WardIcon fontSize="small" /> },
    { label: 'Roll Management', path: '/admin/import', icon: <ImportIcon fontSize="small" /> },
  ];

  return (
    <AppBar position="sticky" elevation={0} sx={{ borderBottom: '1px solid #1e293b' }}>
      {/* Top Government Tri-color Accent Strip */}
      <Box sx={{ height: 3, display: 'flex', width: '100%' }}>
        <Box sx={{ flex: 1, backgroundColor: '#ff9933' }} />
        <Box sx={{ flex: 1, backgroundColor: '#ffffff' }} />
        <Box sx={{ flex: 1, backgroundColor: '#138808' }} />
      </Box>

      {/* Top Executive Micro-Bar */}
      <Box
        sx={{
          backgroundColor: '#0a0f1d',
          py: 0.6,
          px: 3,
          borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
          fontSize: '0.75rem',
          color: '#94a3b8',
        }}
      >
        <Container maxWidth="xl" disableGutters sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" sx={{ color: '#cbd5e1', fontWeight: 600, letterSpacing: '0.02em' }}>
              STATE ELECTION COMMISSION • ANDHRA PRADESH
            </Typography>
            <Typography variant="caption" sx={{ color: '#64748b' }}>|</Typography>
            <Typography variant="caption" sx={{ color: '#94a3b8' }}>
              Madanuru Gram Panchayat • Kothapatnam Mandal • Prakasam District (108-Ongole)
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: '#cbd5e1' }}>
              <ClockIcon sx={{ fontSize: 13, color: '#38bdf8' }} />
              <Typography variant="caption" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                {timeStr}
              </Typography>
            </Box>
            <Chip
              icon={<DotIcon sx={{ fontSize: '10px !important', color: '#4ade80 !important' }} />}
              label="Offline Desk • Ready"
              size="small"
              sx={{
                height: 20,
                fontSize: '0.68rem',
                backgroundColor: 'rgba(34, 197, 94, 0.15)',
                color: '#4ade80',
                border: '1px solid rgba(34, 197, 94, 0.3)',
                fontWeight: 700,
              }}
            />
          </Box>
        </Container>
      </Box>

      {/* Main Navigation Toolbar */}
      <Box sx={{ backgroundColor: '#0f172a', py: 1 }}>
        <Container maxWidth="xl" disableGutters sx={{ px: { xs: 2, md: 3 } }}>
          <Toolbar disableGutters sx={{ minHeight: '64px !important', display: 'flex', justifyContent: 'space-between' }}>
            {/* Brand Logo & Title */}
            <Box
              component={RouterLink}
              to="/"
              sx={{
                display: 'flex',
                alignItems: 'center',
                textDecoration: 'none',
                gap: 1.8,
              }}
            >
              <Box
                sx={{
                  width: 44,
                  height: 44,
                  borderRadius: 2,
                  backgroundColor: 'linear-gradient(135deg, #1e40af 0%, #3b82f6 100%)',
                  background: 'linear-gradient(135deg, #1d4ed8 0%, #1e3a8a 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  boxShadow: '0 4px 10px rgba(29, 78, 216, 0.35)',
                  border: '1px solid rgba(255, 255, 255, 0.15)',
                }}
              >
                <ShieldIcon sx={{ fontSize: 26, color: '#ffffff' }} />
              </Box>
              <Box>
                <Typography
                  variant="h5"
                  sx={{
                    fontWeight: 900,
                    color: '#ffffff',
                    letterSpacing: '0.04em',
                    lineHeight: 1.1,
                    fontSize: '1.25rem',
                  }}
                >
                  MADANURU VOTER SEVA
                </Typography>
                <Typography
                  variant="caption"
                  sx={{
                    color: '#94a3b8',
                    fontSize: '0.72rem',
                    fontWeight: 500,
                    letterSpacing: '0.01em',
                    display: 'block',
                    mt: 0.3,
                  }}
                >
                  Fast and Reliable Voter Information Lookup • 2026 Roll
                </Typography>
              </Box>
            </Box>

            {/* Navigation Tabs */}
            <Box sx={{ display: 'flex', gap: 1 }}>
              {navItems.map((item) => {
                const active = location.pathname === item.path;
                return (
                  <Button
                    key={item.path}
                    component={RouterLink}
                    to={item.path}
                    startIcon={item.icon}
                    sx={{
                      color: active ? '#ffffff' : '#cbd5e1',
                      backgroundColor: active ? 'rgba(59, 130, 246, 0.2)' : 'transparent',
                      border: active ? '1px solid rgba(59, 130, 246, 0.4)' : '1px solid transparent',
                      fontWeight: active ? 700 : 500,
                      fontSize: '0.88rem',
                      px: 2,
                      py: 0.9,
                      borderRadius: 2,
                      boxShadow: active ? '0 2px 8px rgba(0, 0, 0, 0.25)' : 'none',
                      '&:hover': {
                        backgroundColor: 'rgba(255, 255, 255, 0.08)',
                        color: '#ffffff',
                      },
                    }}
                  >
                    {item.label}
                  </Button>
                );
              })}
            </Box>

            {/* Assistance Desk Unit Chip */}
            <Box sx={{ display: { xs: 'none', lg: 'flex' }, alignItems: 'center', gap: 1.5 }}>
              <Box
                sx={{
                  px: 1.5,
                  py: 0.6,
                  borderRadius: 2,
                  backgroundColor: 'rgba(255, 255, 255, 0.05)',
                  border: '1px solid rgba(255, 255, 255, 0.1)',
                  textAlign: 'right',
                }}
              >
                <Typography variant="caption" sx={{ color: '#e2e8f0', fontWeight: 700, display: 'block', fontSize: '0.75rem' }}>
                  Desk Assistant 01
                </Typography>
                <Typography variant="caption" sx={{ color: '#64748b', fontSize: '0.68rem', display: 'block' }}>
                  GP Office Madanuru
                </Typography>
              </Box>
            </Box>
          </Toolbar>
        </Container>
      </Box>
    </AppBar>
  );
};
