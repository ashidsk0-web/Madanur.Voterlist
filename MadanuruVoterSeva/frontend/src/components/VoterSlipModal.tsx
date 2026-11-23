import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Divider,
  Grid,
} from '@mui/material';
import {
  Print as PrintIcon,
  Close as CloseIcon,
  VerifiedUser as ShieldIcon,
  WarningAmber as WarningIcon,
} from '@mui/icons-material';
import type { VoterDetail } from '../types';

interface VoterSlipModalProps {
  open: boolean;
  onClose: () => void;
  voter: VoterDetail | null;
}

export const VoterSlipModal: React.FC<VoterSlipModalProps> = ({ open, onClose, voter }) => {
  if (!voter) return null;

  const handlePrint = () => {
    window.print();
  };

  const isFemale = voter.gender === 'స్త్రీ' || voter.gender?.toLowerCase().includes('female') || voter.gender === 'F';

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogContent sx={{ p: 3, backgroundColor: '#ffffff' }}>
        {/* Printable Slip Container */}
        <Box
          id="printable-slip"
          sx={{
            p: 2.5,
            border: '2px dashed #0f172a',
            borderRadius: 2,
            backgroundColor: '#ffffff',
          }}
        >
          {/* Slip Header */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.2, mb: 1.5 }}>
            <Box
              sx={{
                width: 32,
                height: 32,
                borderRadius: 1,
                backgroundColor: '#0f172a',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#ffffff',
              }}
            >
              <ShieldIcon sx={{ fontSize: 18 }} />
            </Box>
            <Box sx={{ flexGrow: 1 }}>
              <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '0.85rem', letterSpacing: '0.02em' }}>
                MADANURU GRAM PANCHAYAT • VOTER ASSISTANCE SLIP
              </Typography>
              <Typography sx={{ color: '#64748b', fontSize: '0.7rem' }}>
                General Elections 2026 • 108-Ongole Assembly • Kothapatnam Mandal
              </Typography>
            </Box>
          </Box>

          <Divider sx={{ mb: 2, borderColor: '#0f172a' }} />

          {/* Body: Photo (Left) + Primary Details (Right) */}
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', mb: 2 }}>
            {/* Photo Box */}
            <Box
              sx={{
                width: 100,
                height: 120,
                borderRadius: 1,
                border: '1.5px solid #cbd5e1',
                overflow: 'hidden',
                backgroundColor: '#f1f5f9',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
              }}
            >
              <svg viewBox="0 0 100 120" width="100%" height="100%">
                <rect width="100" height="120" fill="#f1f5f9" />
                {isFemale ? (
                  <g transform="translate(5, 5)">
                    <path d="M 20 12 C 8 12, 6 35, 6 55 C 6 75, 15 80, 20 80 C 65 80, 80 75, 80 55 C 80 35, 78 12, 65 12 Z" fill="#334155" />
                    <rect x="36" y="48" width="14" height="18" fill="#fbbf24" />
                    <path d="M 10 95 C 10 75, 25 68, 43 68 C 61 68, 76 75, 76 95 Z" fill="#991b1b" />
                    <ellipse cx="43" cy="38" rx="17" ry="20" fill="#fbbf24" />
                  </g>
                ) : (
                  <g transform="translate(5, 5)">
                    <rect x="36" y="46" width="14" height="18" fill="#fbbf24" />
                    <path d="M 12 105 C 12 78, 25 72, 43 72 C 61 72, 74 78, 74 105 Z" fill="#1e293b" />
                    <ellipse cx="43" cy="35" rx="18" ry="21" fill="#fbbf24" />
                    <path d="M 23 30 C 23 12, 35 10, 43 10 C 51 10, 63 12, 63 30 Z" fill="#0f172a" />
                  </g>
                )}
                <rect y="106" width="100" height="14" fill="#0f172a" />
                <text x="50" y="116" fontSize="6.5" fontWeight="700" textAnchor="middle" fill="#ffffff">
                  PHOTO ON RECORD
                </text>
              </svg>
            </Box>

            {/* Core Details */}
            <Box sx={{ flexGrow: 1 }}>
              {/* Highlight Serial & Ward */}
              <Box sx={{ display: 'flex', gap: 1, mb: 1.5 }}>
                <Box sx={{ flex: 1, p: 1, backgroundColor: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 1, textAlign: 'center' }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.62rem', textTransform: 'uppercase' }}>
                    GP Serial No
                  </Typography>
                  <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.25rem', lineHeight: 1.1 }}>
                    #{voter.gramPanchayatSerialNumber}
                  </Typography>
                </Box>

                <Box sx={{ flex: 1, p: 1, backgroundColor: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 1, textAlign: 'center' }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.62rem', textTransform: 'uppercase' }}>
                    Ward Number
                  </Typography>
                  <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.25rem', lineHeight: 1.1 }}>
                    Ward {voter.wardNumber}
                  </Typography>
                </Box>
              </Box>

              {/* Voter Name */}
              <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase' }}>
                Voter Name (Electoral Roll):
              </Typography>
              <Typography className="telugu-text" sx={{ fontWeight: 800, color: '#0f172a', fontSize: '1.1rem', mb: 0.8 }}>
                {voter.originalName}
              </Typography>

              {/* Relative & House */}
              <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase' }}>
                Relative / Father / Husband:
              </Typography>
              <Typography className="telugu-text" sx={{ fontWeight: 700, color: '#1e293b', fontSize: '0.85rem', mb: 0.8 }}>
                {voter.relativeName || '—'}
              </Typography>

              <Grid container spacing={1}>
                <Grid size={{ xs: 6 }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase' }}>
                    House Number:
                  </Typography>
                  <Typography sx={{ fontWeight: 700, color: '#0f172a', fontSize: '0.85rem' }}>
                    #{voter.houseNumber || '—'}
                  </Typography>
                </Grid>
                <Grid size={{ xs: 6 }}>
                  <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase' }}>
                    EPIC ID:
                  </Typography>
                  <Typography className="epic-mono" sx={{ fontWeight: 800, color: '#0f172a', fontSize: '0.85rem' }}>
                    {voter.maskedEPIC}
                  </Typography>
                </Grid>
              </Grid>
            </Box>
          </Box>

          <Divider sx={{ my: 1.5 }} />

          {/* Assembly Reference */}
          <Box sx={{ p: 1, backgroundColor: '#f8fafc', borderRadius: 1, mb: 1.5, border: '1px solid #e2e8f0' }}>
            <Typography sx={{ color: '#64748b', fontWeight: 700, fontSize: '0.65rem', textTransform: 'uppercase' }}>
              Assembly Reference (Distinct from GP Roll Serial)
            </Typography>
            <Typography sx={{ color: '#334155', fontWeight: 600, fontSize: '0.78rem' }}>
              Assembly 108-Ongole • Part #{voter.assemblyPartNumber || 'N/A'} • Serial #{voter.assemblySerialNumber || 'N/A'} • Roll Page {voter.sourcePageNumber}
            </Typography>
          </Box>

          {/* Polling Station Notice */}
          <Box sx={{ p: 1, borderRadius: 1, border: '1px solid #fed7aa', backgroundColor: '#fffbeb', mb: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.2 }}>
              <WarningIcon sx={{ color: '#d97706', fontSize: 14 }} />
              <Typography sx={{ fontWeight: 700, color: '#9a3412', fontSize: '0.68rem', textTransform: 'uppercase' }}>
                Polling Station Notice
              </Typography>
            </Box>
            <Typography sx={{ color: '#7c2d12', fontSize: '0.68rem', lineHeight: 1.35 }}>
              Final polling station allocations are subject to Returning Officer gazette notification on election day.
            </Typography>
          </Box>

          {/* Footer Timestamp */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', pt: 0.5 }}>
            <Typography sx={{ color: '#94a3b8', fontSize: '0.65rem' }}>
              Issued by Madanuru GP Assistance Desk • Electoral Roll 2026
            </Typography>
            <Typography sx={{ color: '#94a3b8', fontSize: '0.65rem' }}>
              {new Date().toLocaleDateString('en-IN')} {new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })}
            </Typography>
          </Box>
        </Box>
      </DialogContent>

      <DialogActions sx={{ p: 1.5, px: 3, backgroundColor: '#f8fafc', borderTop: '1px solid #e2e8f0' }}>
        <Button onClick={onClose} variant="outlined" size="small" startIcon={<CloseIcon sx={{ fontSize: 14 }} />}>
          Close
        </Button>
        <Button onClick={handlePrint} variant="contained" size="small" startIcon={<PrintIcon sx={{ fontSize: 14 }} />} sx={{ backgroundColor: '#0f172a' }}>
          Print Slip
        </Button>
      </DialogActions>
    </Dialog>
  );
};
