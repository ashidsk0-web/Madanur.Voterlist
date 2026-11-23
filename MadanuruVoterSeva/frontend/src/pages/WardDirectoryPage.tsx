import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Pagination,
  TextField,
  InputAdornment,
} from '@mui/material';
import {
  AccountBalance as WardIcon,
  People as VotersIcon,
  Visibility as ViewIcon,
  ArrowBack as BackIcon,
  CheckCircle as CheckIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import type { WardSummary, VoterSearchResult, PagedResult } from '../types';

export const WardDirectoryPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [wards, setWards] = useState<WardSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filter query for wards
  const [filterQuery, setFilterQuery] = useState('');

  const wardNumber = Number(searchParams.get('ward'));
  const page = Math.max(1, Number.parseInt(searchParams.get('page') ?? '1', 10) || 1);
  const selectedWard = wards.find(ward => ward.wardNumber === wardNumber);
  const wardId = selectedWard?.wardId;
  const [roster, setRoster] = useState<{
    wardId: number;
    page: number;
    data?: PagedResult<VoterSearchResult>;
    error?: string;
  }>();
  const currentRoster = roster?.wardId === wardId && roster?.page === page ? roster : undefined;
  const wardVoters = currentRoster?.data;
  const votersLoading = wardId !== undefined && !currentRoster;
  const pageError = error || currentRoster?.error;

  useEffect(() => {
    let cancelled = false;
    api.getWards()
      .then(data => { if (!cancelled) setWards(data); })
      .catch(() => { if (!cancelled) setError('Failed to load ward directory.'); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (wardId === undefined) return;
    let cancelled = false;
    api.getVotersByWard(wardId, page, 25)
      .then(data => { if (!cancelled) setRoster({ wardId, page, data }); })
      .catch(() => {
        if (!cancelled) setRoster({ wardId, page, error: 'Error loading voters in ward.' });
      });
    return () => { cancelled = true; };
  }, [wardId, page]);

  // Filtered wards
  const displayedWards = wards.filter((w) =>
    `Ward ${w.wardNumber}`.toLowerCase().includes(filterQuery.toLowerCase()) ||
    w.wardNumber.toString().includes(filterQuery)
  );

  return (
    <Box sx={{ width: '100%' }}>
      {/* Executive Ward Header */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1.5,
          mb: 2,
        }}
      >
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '1.15rem' }}>
              Ward Directory
            </Typography>
            <Chip
              icon={<WardIcon sx={{ fontSize: '12px !important' }} />}
              label="14 Wards"
              size="small"
              sx={{ height: 20, fontSize: '0.68rem', fontWeight: 600, backgroundColor: '#f1f5f9', color: '#334155' }}
            />
          </Box>
          <Typography sx={{ color: '#64748b', fontSize: '0.78rem' }}>
            Elector distribution and serial breakdown for Madanuru Gram Panchayat (2026)
          </Typography>
        </Box>

        {selectedWard ? (
          <Button
            variant="outlined"
            size="small"
            startIcon={<BackIcon sx={{ fontSize: 14 }} />}
            onClick={() => setSearchParams({})}
            sx={{ height: 30, fontSize: '0.75rem', fontWeight: 600 }}
          >
            All 14 Wards
          </Button>
        ) : (
          <Box sx={{ width: { xs: '100%', sm: 220 } }}>
            <TextField
              size="small"
              fullWidth
              label="Filter wards"
              placeholder="Example: 5"
              value={filterQuery}
              onChange={(e) => setFilterQuery(e.target.value)}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <FilterIcon sx={{ color: '#64748b', fontSize: 15 }} />
                    </InputAdornment>
                  ),
                },
              }}
            />
          </Box>
        )}
      </Box>

      {pageError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 1.5, py: 0.5, fontSize: '0.78rem' }}>
          {pageError}
        </Alert>
      )}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress size={24} />
        </Box>
      ) : !selectedWard ? (
        /* 14 WARDS COMPACT GRID */
        <Grid container spacing={1.5}>
          {displayedWards.map((ward) => (
            <Grid key={ward.wardId} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
              <Card
                sx={{
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  borderRadius: 1.5,
                  border: '1px solid #e2e8f0',
                  boxShadow: 'none',
                  transition: 'all 0.1s ease',
                  '&:hover': {
                    borderColor: '#cbd5e1',
                    boxShadow: '0 2px 6px rgba(0,0,0,0.04)',
                  },
                }}
              >
                <CardContent sx={{ flexGrow: 1, p: 1.5 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                    <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.9rem' }}>
                      Ward {ward.wardNumber}
                    </Typography>
                    <Chip
                      label="Madanuru"
                      size="small"
                      sx={{ height: 18, fontSize: '0.62rem', backgroundColor: '#f8fafc', color: '#64748b' }}
                    />
                  </Box>

                  <Box sx={{ my: 0.8 }}>
                    <Typography sx={{ color: '#64748b', fontWeight: 600, fontSize: '0.65rem', textTransform: 'uppercase' }}>
                      Electors
                    </Typography>
                    <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.25rem', lineHeight: 1.1 }}>
                      {ward.totalVoters}
                    </Typography>
                  </Box>

                  <Box sx={{ py: 0.6, px: 1, backgroundColor: '#f8fafc', borderRadius: 1, border: '1px solid #f1f5f9', mb: 1 }}>
                    <Typography sx={{ color: '#64748b', fontSize: '0.68rem' }}>
                      GP Serial: <strong>#{ward.minSerialNumber} – #{ward.maxSerialNumber}</strong>
                    </Typography>
                  </Box>

                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: '#16a34a' }}>
                    <CheckIcon sx={{ fontSize: 13 }} />
                    <Typography sx={{ fontWeight: 600, fontSize: '0.68rem' }}>
                      {ward.verifiedVoters} Verified
                    </Typography>
                  </Box>
                </CardContent>

                <CardActions sx={{ p: 1.5, pt: 0 }}>
                  <Button
                    fullWidth
                    variant="outlined"
                    size="small"
                    startIcon={<ViewIcon sx={{ fontSize: 13 }} />}
                    onClick={() => setSearchParams({ ward: String(ward.wardNumber) })}
                    sx={{ height: 28, fontSize: '0.72rem', fontWeight: 600 }}
                  >
                    View Roster
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      ) : (
        /* SINGLE WARD VOTERS ROSTER TABLE */
        <Box>
          <Box
            sx={{
              p: 1.5,
              backgroundColor: '#ffffff',
              borderRadius: 1.5,
              border: '1px solid #e2e8f0',
              mb: 1.5,
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: 1,
            }}
          >
            <Box>
              <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.95rem' }}>
                Ward {selectedWard.wardNumber} Voter Roster
              </Typography>
              <Typography sx={{ color: '#64748b', fontSize: '0.72rem' }}>
                Total: <strong>{selectedWard.totalVoters}</strong> Electors • Serial Range: <strong>#{selectedWard.minSerialNumber} – #{selectedWard.maxSerialNumber}</strong>
              </Typography>
            </Box>
            <Chip
              icon={<VotersIcon sx={{ fontSize: '12px !important' }} />}
              label={`${wardVoters?.totalCount || selectedWard.totalVoters} Voters`}
              size="small"
              sx={{ height: 22, fontSize: '0.72rem', fontWeight: 600 }}
            />
          </Box>

          {votersLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
              <CircularProgress size={24} />
            </Box>
          ) : wardVoters ? (
            <>
              <TableContainer component={Paper} sx={{ borderRadius: 1.5, border: '1px solid #e2e8f0', mb: 2, boxShadow: 'none' }}>
                <Table size="small" className="desk-card-table" role="table">
                  <TableHead role="rowgroup">
                    <TableRow role="row">
                      <TableCell role="columnheader">GP #</TableCell>
                      <TableCell role="columnheader">Voter Name (Telugu)</TableCell>
                      <TableCell role="columnheader">Relative / Father</TableCell>
                      <TableCell role="columnheader">House</TableCell>
                      <TableCell role="columnheader">Masked EPIC</TableCell>
                      <TableCell role="columnheader">Page</TableCell>
                      <TableCell role="columnheader">Station</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody role="rowgroup">
                    {wardVoters.items.map((v) => (
                      <TableRow role="row" key={v.voterId} hover sx={{ '&:last-child td, &:last-child th': { border: 0 } }}>
                        <TableCell role="cell" data-label="GP serial" sx={{ fontWeight: 700, color: '#0f172a' }}>
                          #{v.gramPanchayatSerialNumber}
                        </TableCell>
                        <TableCell role="cell" data-label="Voter name" className="telugu-text desk-card-title" sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.88rem' }}>
                          {v.originalName}
                        </TableCell>
                        <TableCell role="cell" data-label="Relative" sx={{ color: '#64748b' }}>{v.relativeName || '—'}</TableCell>
                        <TableCell role="cell" data-label="House" sx={{ fontWeight: 600 }}>{v.houseNumber || '—'}</TableCell>
                        <TableCell role="cell" data-label="Masked EPIC">
                          <span className="epic-mono" style={{ fontSize: '0.75rem', color: '#334155' }}>
                            {v.maskedEPIC}
                          </span>
                        </TableCell>
                        <TableCell role="cell" data-label="PDF page" sx={{ color: '#64748b' }}>Page {v.sourcePageNumber}</TableCell>
                        <TableCell role="cell" data-label="Polling station">
                          {v.pollingStationVerificationStatus === 'Verified' ? (
                            <Chip label="Verified" size="small" sx={{ height: 18, fontSize: '0.62rem', backgroundColor: '#ecfdf5', color: '#065f46' }} />
                          ) : (
                            <Chip label="Unverified" size="small" variant="outlined" sx={{ height: 18, fontSize: '0.62rem', color: '#d97706', borderColor: '#fde68a' }} />
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>

              {wardVoters.totalPages > 1 && (
                <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
                  <Pagination
                    className="desk-pagination"
                    siblingCount={0}
                    count={wardVoters.totalPages}
                    page={page}
                    onChange={(_, val) => setSearchParams({ ward: String(wardNumber), page: String(val) })}
                    color="primary"
                    size="small"
                  />
                </Box>
              )}
            </>
          ) : null}
        </Box>
      )}
    </Box>
  );
};
