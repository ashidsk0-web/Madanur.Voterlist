import { createTheme } from '@mui/material/styles';

export const enterpriseTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#0f172a', // Deep Slate 900
      light: '#334155',
      dark: '#020617',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#2563eb', // Clean Royal Accent
      light: '#60a5fa',
      dark: '#1d4ed8',
      contrastText: '#ffffff',
    },
    success: {
      main: '#059669', // Muted Emerald
      light: '#34d399',
      dark: '#065f46',
      contrastText: '#ffffff',
    },
    warning: {
      main: '#d97706', // Amber
      light: '#f59e0b',
      dark: '#b45309',
      contrastText: '#ffffff',
    },
    error: {
      main: '#dc2626',
      light: '#ef4444',
      dark: '#b91c1c',
    },
    background: {
      default: '#f8fafc',
      paper: '#ffffff',
    },
    text: {
      primary: '#0f172a',
      secondary: '#64748b',
    },
    divider: '#e2e8f0',
  },
  typography: {
    fontFamily: [
      '"Plus Jakarta Sans"',
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: {
      fontSize: '1.5rem', // 24px
      fontWeight: 700,
      letterSpacing: '-0.02em',
      color: '#0f172a',
    },
    h2: {
      fontSize: '1.25rem', // 20px
      fontWeight: 700,
      letterSpacing: '-0.015em',
      color: '#0f172a',
    },
    h3: {
      fontSize: '1.125rem', // 18px
      fontWeight: 700,
      letterSpacing: '-0.01em',
      color: '#0f172a',
    },
    h4: {
      fontSize: '1rem', // 16px
      fontWeight: 700,
      letterSpacing: '-0.01em',
      color: '#0f172a',
    },
    h5: {
      fontSize: '0.9375rem', // 15px
      fontWeight: 600,
      color: '#1e293b',
    },
    h6: {
      fontSize: '0.875rem', // 14px
      fontWeight: 600,
      color: '#1e293b',
    },
    subtitle1: {
      fontSize: '0.875rem',
      fontWeight: 600,
      color: '#334155',
    },
    subtitle2: {
      fontSize: '0.75rem',
      fontWeight: 600,
      letterSpacing: '0.02em',
      color: '#64748b',
    },
    body1: {
      fontSize: '0.84375rem', // ~13.5px
      lineHeight: 1.5,
      color: '#334155',
    },
    body2: {
      fontSize: '0.78125rem', // ~12.5px
      lineHeight: 1.45,
      color: '#64748b',
    },
    caption: {
      fontSize: '0.6875rem', // ~11px
      fontWeight: 500,
      color: '#64748b',
    },
    button: {
      textTransform: 'none',
      fontWeight: 600,
      fontSize: '0.8125rem', // 13px
    },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 6,
          padding: '6px 14px',
          fontSize: '0.8125rem',
          boxShadow: 'none',
          transition: 'all 0.12s ease',
          '&:hover': {
            boxShadow: '0 2px 6px rgba(15, 23, 42, 0.12)',
          },
        },
        contained: {
          backgroundColor: '#0f172a',
          color: '#ffffff',
          '&:hover': {
            backgroundColor: '#1e293b',
          },
        },
        outlined: {
          borderColor: '#cbd5e1',
          color: '#334155',
          '&:hover': {
            borderColor: '#94a3b8',
            backgroundColor: '#f8fafc',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          border: '1px solid #e2e8f0',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.04)',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          height: 22,
          fontSize: '0.72rem',
          fontWeight: 600,
          borderRadius: 4,
        },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          backgroundColor: '#f8fafc',
          '& .MuiTableCell-root': {
            color: '#475569',
            fontWeight: 700,
            fontSize: '0.72rem',
            textTransform: 'uppercase',
            letterSpacing: '0.04em',
            padding: '8px 12px',
            borderBottom: '1px solid #e2e8f0',
          },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          padding: '8px 12px',
          borderColor: '#f1f5f9',
          fontSize: '0.8125rem',
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 6,
          fontSize: '0.84375rem',
          backgroundColor: '#ffffff',
          '& fieldset': {
            borderColor: '#cbd5e1',
          },
          '&:hover fieldset': {
            borderColor: '#94a3b8',
          },
          '&.Mui-focused fieldset': {
            borderColor: '#0f172a',
            borderWidth: 1.5,
          },
        },
        input: {
          padding: '8.5px 12px',
        },
      },
    },
  },
});
