import { DeskSession } from './components/DeskSession';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { enterpriseTheme } from './theme/theme';
import { AppShell } from './layouts/AppShell';
import { AppRoutes } from './routes/AppRoutes';

export const App: React.FC = () => {
  return (
    <ThemeProvider theme={enterpriseTheme}>
      <CssBaseline />
      <BrowserRouter>
        <DeskSession>{signOut => (
          <AppShell onSignOut={signOut}>
            <AppRoutes />
          </AppShell>
        )}</DeskSession>
      </BrowserRouter>
    </ThemeProvider>
  );
};

export default App;
