import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Alert,
  ToggleButtonGroup,
  ToggleButton,
} from '@mui/material';
import {
  Print as PrintIcon,
  Shield as ShieldIcon,
  WarningAmber as WarningIcon,
  ContentCopy as CopyIcon,
  Description as RollIcon,
  Badge as IdIcon,
  HowToVote as VoteIcon,
} from '@mui/icons-material';
import type { VoterSearchResult } from '../types';
import { OfficialRollCard } from './OfficialRollCard';

interface VoterDossierProps {
  voter: VoterSearchResult | null;
  onPrintSlip: (voter: VoterSearchResult) => void;
}

export const VoterDossier: React.FC<VoterDossierProps> = ({ voter, onPrintSlip }) => {
  const [format, setFormat] = useState<'roll' | 'card'>('roll');
  const [copied, setCopied] = useState(false);

  if (!voter) {
    return (
      <Card
        sx={{
          height: '100%',
          minHeight: 400,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          p: 3,
          textAlign: 'center',
          backgroundColor: '#ffffff',
          borderRadius: 2,
          border: '1.5px dashed #cbd5e1',
          boxShadow: 'none',
        }}
      >
        <Box
          sx={{
            width: 48,
            height: 48,
            borderRadius: '50%',
            backgroundColor: '#f1f5f9',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#64748b',
            mb: 1.5,
          }}
        >
          <VoteIcon sx={{ fontSize: 24, color: '#64748b' }} />
        </Box>
        <Typography sx={{ fontWeight: 700, color: '#1e293b', fontSize: '0.9rem', mb: 0.5 }}>
          No Elector Record Selected
        </Typography>
        <Typography sx={{ color: '#64748b', maxWidth: 280, fontSize: '0.78rem', lineHeight: 1.45 }}>
          Search an <strong>EPIC ID</strong> or select any candidate on the left to inspect their official electoral roll entry.
        </Typography>
      </Card>
    );
  }

  const handleCopy = () => {
    const text = `గ్రా.పం.ఓ.జా.వ.సం.: #${voter.gramPanchayatSerialNumber}\nఅ.ఓ.జా.వ.సం.: 108 - ${voter.assemblyPartNumber ?? '—'} - ${voter.assemblySerialNumber ?? '—'}\nఓటరు పేరు: ${voter.originalName}\nతండ్రి/భర్త పేరు: ${voter.relativeName || '—'}\nఇంటి సంఖ్య: ${voter.houseNumber || '—'}\nలింగము: ${voter.gender || '—'}\nఓటరు ఐడి. కార్డు నెం.: ${voter.maskedEPIC}\nవార్డు: Ward ${voter.wardNumber}\nమూల పేజీ: Page ${voter.sourcePageNumber} of Madanuru 2026 Roll`;
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Card
      sx={{
        borderRadius: 2,
        border: '1px solid #cbd5e1',
        boxShadow: '0 4px 16px rgba(15, 23, 42, 0.08)',
        backgroundColor: '#ffffff',
        overflow: 'hidden',
      }}
    >
      {/* Top Format Selector Ribbon */}
      <Box
        sx={{
          px: 2,
          py: 1,
          backgroundColor: '#0f172a',
          color: '#ffffff',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1,
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <ShieldIcon sx={{ color: '#38bdf8', fontSize: 18 }} />
          <Typography sx={{ fontWeight: 800, fontSize: '0.8rem', color: '#ffffff' }}>
            Electoral Roll 2026 • Madanuru GP
          </Typography>
        </Box>

        {/* View Switcher: Official Roll vs e-EPIC PVC */}
        <ToggleButtonGroup
          value={format}
          exclusive
          onChange={(_, val) => {
            if (val) setFormat(val);
          }}
          size="small"
          sx={{
            height: 24,
            backgroundColor: 'rgba(255, 255, 255, 0.1)',
            borderRadius: 1,
            '& .MuiToggleButton-root': {
              px: 1,
              py: 0.2,
              fontSize: '0.68rem',
              fontWeight: 600,
              color: '#94a3b8',
              border: 'none',
              '&.Mui-selected': {
                backgroundColor: '#ffffff',
                color: '#0f172a',
                fontWeight: 700,
              },
            },
          }}
        >
          <ToggleButton value="roll">
            <RollIcon sx={{ fontSize: 13, mr: 0.5 }} />
            Official Roll Card
          </ToggleButton>
          <ToggleButton value="card">
            <IdIcon sx={{ fontSize: 13, mr: 0.5 }} />
            e-EPIC ID
          </ToggleButton>
        </ToggleButtonGroup>
      </Box>

      <CardContent sx={{ p: 2 }}>
        {/* CENTERPIECE: THE AUTHENTIC ROLL CARD (EXACT MATCH TO PDF) */}
        <Box sx={{ mb: 2 }}>
          <OfficialRollCard voter={voter} compact={false} />
        </Box>

        {/* Polling Station Alert Notice */}
        <Alert
          severity={voter.pollingStationVerificationStatus === 'Verified' ? 'success' : 'warning'}
          icon={<WarningIcon sx={{ fontSize: 14 }} />}
          sx={{ mb: 2, borderRadius: 1.5, py: 0.5, px: 1.2, fontSize: '0.72rem' }}
        >
          {voter.pollingStationVerificationStatus === 'Verified' ? (
            <>
              <strong>Station Verified:</strong> {voter.pollingStationName} ({voter.pollingStationAddress})
            </>
          ) : (
            <>
              <strong>Polling Station:</strong> Pending Returning Officer gazette notification on poll day. Do not assume polling station by ward number.
            </>
          )}
        </Alert>

        {/* Action Controls */}
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            fullWidth
            variant="contained"
            color="primary"
            startIcon={<PrintIcon sx={{ fontSize: 15 }} />}
            onClick={() => onPrintSlip(voter)}
            sx={{
              height: 38,
              fontWeight: 700,
              fontSize: '0.82rem',
              borderRadius: 1.5,
              backgroundColor: '#0f172a',
              '&:hover': {
                backgroundColor: '#1e293b',
              },
            }}
          >
            Print Voter Assistance Slip
          </Button>

          <Button
            variant="outlined"
            startIcon={<CopyIcon sx={{ fontSize: 14 }} />}
            onClick={handleCopy}
            sx={{
              height: 38,
              px: 2,
              fontWeight: 600,
              fontSize: '0.78rem',
              borderRadius: 1.5,
              borderColor: '#cbd5e1',
              color: '#334155',
            }}
          >
            {copied ? 'Copied' : 'Copy'}
          </Button>
        </Box>
      </CardContent>
    </Card>
  );
};
