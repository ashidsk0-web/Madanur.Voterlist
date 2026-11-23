import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  CircularProgress,
  Alert,
  Divider,
  Chip,
} from '@mui/material';
import {
  HowToVote as VoterIcon,
  AccountBalance as WardIcon,
  VerifiedUser as VerifiedIcon,
  CalendarToday as DateIcon,
  Search as SearchIcon,
  Refresh as RefreshIcon,
  FactCheck as RollIcon,
} from '@mui/icons-material';
import { Link as RouterLink } from 'react-router-dom';
import { api } from '../api/client';
import type { DashboardStatistics } from '../types';

export const DashboardPage: React.FC = () => {
  const [stats, setStats] = useState<DashboardStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getStatistics();
      setStats(data);
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'Failed to load dashboard statistics.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStats();
  }, []);

  return (
    <Box sx={{ width: '100%' }}>
      {/* Executive Page Header */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1.5,
          mb: 2.5,
        }}
      >
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '1.15rem' }}>
              Operational Dashboard
            </Typography>
            <Chip
              label="Live State"
              size="small"
              sx={{ height: 20, fontSize: '0.68rem', fontWeight: 600, backgroundColor: '#ecfdf5', color: '#065f46' }}
            />
          </Box>
          <Typography sx={{ color: '#64748b', fontSize: '0.78rem' }}>
            Roll status, verified elector metrics, and ward statistics for Madanuru Gram Panchayat (2026)
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          <Button
            variant="outlined"
            size="small"
            startIcon={<RefreshIcon sx={{ fontSize: 14 }} />}
            onClick={fetchStats}
            disabled={loading}
            sx={{ height: 32, fontSize: '0.75rem', fontWeight: 600 }}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            color="primary"
            size="small"
            component={RouterLink}
            to="/"
            startIcon={<SearchIcon sx={{ fontSize: 14 }} />}
            sx={{ height: 32, fontSize: '0.75rem', fontWeight: 600, backgroundColor: '#0f172a' }}
          >
            Open Search Console
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 1.5, py: 0.5, fontSize: '0.78rem' }}>
          {error}
        </Alert>
      )}

      {loading && !stats ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={24} />
        </Box>
      ) : stats ? (
        <>
          {/* Executive Metric Cards */}
          <Grid container spacing={1.5} sx={{ mb: 2.5 }}>
            {/* Total Voters */}
            <Grid size={{ xs: 6, lg: 3 }}>
              <Card sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 1.25, sm: 2 }, '&:last-child': { pb: { xs: 1.25, sm: 2 } } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                    <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      Imported Electors
                    </Typography>
                    <VoterIcon sx={{ fontSize: 16, color: '#64748b' }} />
                  </Box>
                  <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.4rem', lineHeight: 1.2, my: 0.3 }}>
                    {stats.totalImportedVoters.toLocaleString()}
                  </Typography>
                  <Typography sx={{ color: '#059669', fontWeight: 600, fontSize: '0.68rem' }}>
                    100% Reconciled to Roll
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Total Wards */}
            <Grid size={{ xs: 6, lg: 3 }}>
              <Card sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 1.25, sm: 2 }, '&:last-child': { pb: { xs: 1.25, sm: 2 } } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                    <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      Panchayat Wards
                    </Typography>
                    <WardIcon sx={{ fontSize: 16, color: '#64748b' }} />
                  </Box>
                  <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.4rem', lineHeight: 1.2, my: 0.3 }}>
                    {stats.totalWards}
                  </Typography>
                  <Typography sx={{ color: '#64748b', fontSize: '0.68rem' }}>
                    W1–W13 (395 ea) • W14 (408)
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Records Pending Verification */}
            <Grid size={{ xs: 6, lg: 3 }}>
              <Card sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 1.25, sm: 2 }, '&:last-child': { pb: { xs: 1.25, sm: 2 } } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                    <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      Pending Queue
                    </Typography>
                    <VerifiedIcon sx={{ fontSize: 16, color: '#059669' }} />
                  </Box>
                  <Typography sx={{ fontWeight: 800, color: '#059669', fontSize: '1.4rem', lineHeight: 1.2, my: 0.3 }}>
                    {stats.recordsPendingVerification}
                  </Typography>
                  <Typography sx={{ color: '#059669', fontWeight: 600, fontSize: '0.68rem' }}>
                    Zero Unverified Records
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Validation & Roll State */}
            <Grid size={{ xs: 6, lg: 3 }}>
              <Card sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', boxShadow: 'none' }}>
                <CardContent sx={{ p: { xs: 1.25, sm: 2 }, '&:last-child': { pb: { xs: 1.25, sm: 2 } } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                    <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      Roll State
                    </Typography>
                    <RollIcon sx={{ fontSize: 16, color: '#64748b' }} />
                  </Box>
                  <Box sx={{ my: 0.3 }}>
                    <Chip
                      label={stats.importValidationStatus}
                      size="small"
                      sx={{ height: 22, fontSize: '0.72rem', fontWeight: 600, backgroundColor: '#ecfdf5', color: '#065f46' }}
                    />
                  </Box>
                  <Typography sx={{ color: '#64748b', fontSize: '0.68rem', mt: 0.2 }}>
                    Staged: {stats.stagedRecordsCount} | Validated
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          {/* Roll Authority & Metadata */}
          <Card sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', boxShadow: 'none' }}>
            <CardContent sx={{ p: 2.5 }}>
              <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.9rem', mb: 1.5 }}>
                Electoral Roll Publication Authority
              </Typography>
              <Divider sx={{ mb: 2 }} />

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, md: 5 }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 600, fontSize: '0.68rem', textTransform: 'uppercase' }}>
                    Roll Version
                  </Typography>
                  <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.85rem', mt: 0.2 }}>
                    {stats.activeRollVersion}
                  </Typography>
                  <Typography sx={{ color: '#64748b', fontSize: '0.72rem', mt: 0.2 }}>
                    Qualifying Date: <strong>01-01-2026</strong> • State Election Commission
                  </Typography>
                </Grid>

                <Grid size={{ xs: 12, md: 4 }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 600, fontSize: '0.68rem', textTransform: 'uppercase' }}>
                    Jurisdiction Hierarchy
                  </Typography>
                  <Typography sx={{ fontWeight: 600, color: '#1e293b', fontSize: '0.82rem', mt: 0.2 }}>
                    మడనూరు (Madanuru) Gram Panchayat
                  </Typography>
                  <Typography sx={{ color: '#64748b', fontSize: '0.72rem' }}>
                    Kothapatnam Mandal • Prakasam District • 108-Ongole
                  </Typography>
                </Grid>

                <Grid size={{ xs: 12, md: 3 }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 600, fontSize: '0.68rem', textTransform: 'uppercase' }}>
                    Database Journal
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8, mt: 0.2 }}>
                    <DateIcon sx={{ fontSize: 14, color: '#64748b' }} />
                    <Typography sx={{ fontWeight: 600, color: '#0f172a', fontSize: '0.8rem' }}>
                      {stats.lastImportDate || 'Active Session'}
                    </Typography>
                  </Box>
                  <Typography sx={{ color: '#16a34a', fontWeight: 600, fontSize: '0.68rem', mt: 0.2 }}>
                    ● Local SQLite WAL Engine
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </>
      ) : null}
    </Box>
  );
};
