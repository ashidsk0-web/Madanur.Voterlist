import { api } from '../api/client';
import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { VoterSearchPage } from '../pages/VoterSearchPage';
import { DashboardPage } from '../pages/DashboardPage';
import { WardDirectoryPage } from '../pages/WardDirectoryPage';
import { ImportManagementPage } from '../pages/ImportManagementPage';

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<VoterSearchPage />} />
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/wards" element={<WardDirectoryPage />} />
      <Route path="/admin/import" element={api.getCurrentUser().role === 'Admin' ? <ImportManagementPage /> : <Navigate to="/" replace />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};
