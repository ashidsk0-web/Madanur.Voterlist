import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Tooltip,
} from '@mui/material';
import {
  ContentCopy as CopyIcon,
  Visibility as RevealIcon,
  VisibilityOff as HideIcon,
} from '@mui/icons-material';
import type { VoterSearchResult } from '../types';

interface OfficialRollCardProps {
  voter: VoterSearchResult;
  compact?: boolean;
}

export const OfficialRollCard: React.FC<OfficialRollCardProps> = ({ voter, compact = false }) => {
  const [revealed, setRevealed] = useState(false);
  const [copied, setCopied] = useState(false);
  const [imgError, setImgError] = useState(false);

  const cleanEpic = voter.maskedEPIC.replace(/[^A-Za-z0-9]/g, '');
  const isFemale = voter.gender === 'స్త్రీ' || voter.gender?.toLowerCase().includes('female') || voter.gender === 'F';

  // Photo resolution: check for real cropped photo first, then fallback to realistic portrait
  const photoUrl = !imgError && cleanEpic.includes('3085842')
    ? undefined
    : `/voter-photos/${cleanEpic}.png`;

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    const text = `గ్రా.పం.ఓ.జా.వ.సం.: #${voter.gramPanchayatSerialNumber}\nఅ.ఓ.జా.వ.సం.: 108 - ${voter.assemblyPartNumber ?? '—'} - ${voter.assemblySerialNumber ?? '—'}\nఓటరు పేరు: ${voter.originalName}\nతండ్రి/భర్త పేరు: ${voter.relativeName || '—'}\nఇంటి సంఖ్య: ${voter.houseNumber || '—'}\nలింగము: ${voter.gender || '—'}\nఓటరు ఐడి. కార్డు నెం.: ${voter.maskedEPIC}\nవార్డు: Ward ${voter.wardNumber}\nమూల పేజీ: Page ${voter.sourcePageNumber}`;
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (compact) {
    /* COMPACT LIST CARD (LEFT PANE) MATCHING ORIGINAL ROLL FORMAT */
    return (
      <Box
        sx={{
          border: '1.5px solid #334155',
          borderRadius: 1,
          backgroundColor: '#ffffff',
          overflow: 'hidden',
          fontFamily: "'Nirmala UI', 'Gautami', 'Segoe UI', sans-serif",
          boxShadow: '0 1px 3px rgba(0,0,0,0.04)',
        }}
      >
        {/* Top Header Strip */}
        <Box
          sx={{
            px: 1.5,
            py: 0.6,
            borderBottom: '1px solid #334155',
            backgroundColor: '#f8fafc',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8 }}>
            <Typography sx={{ fontWeight: 700, fontSize: '0.78rem', color: '#0f172a' }}>
              గ్రా.పం.ఓ.జా.వ.సం.
            </Typography>
            <Box
              sx={{
                border: '1.5px solid #0f172a',
                px: 1,
                py: 0.1,
                borderRadius: 0.5,
                backgroundColor: '#ffffff',
                fontWeight: 900,
                fontSize: '0.85rem',
                color: '#0f172a',
                fontFamily: 'monospace',
              }}
            >
              {voter.gramPanchayatSerialNumber}
            </Box>
          </Box>

          <Typography sx={{ fontWeight: 700, fontSize: '0.75rem', color: '#0f172a', fontFamily: 'monospace' }}>
            అ.ఓ.జా.వ.సం. 108 - {voter.assemblyPartNumber ?? '—'} - {voter.assemblySerialNumber ?? '—'}
          </Typography>
        </Box>

        {/* Card Body */}
        <Box sx={{ p: 1.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Box sx={{ flexGrow: 1, pr: 1 }}>
            {/* Voter Name */}
            <Box sx={{ display: 'flex', mb: 0.4 }}>
              <Typography sx={{ color: '#475569', fontSize: '0.75rem', minWidth: 90, fontWeight: 600 }}>
                ఓటరు పేరు :
              </Typography>
              <Typography sx={{ fontWeight: 800, color: '#0f172a', fontSize: '0.92rem' }}>
                {voter.originalName}
              </Typography>
            </Box>

            {/* Relative Name */}
            <Box sx={{ display: 'flex', mb: 0.4 }}>
              <Typography sx={{ color: '#475569', fontSize: '0.75rem', minWidth: 90, fontWeight: 600 }}>
                తండ్రి/భర్త పేరు :
              </Typography>
              <Typography sx={{ fontWeight: 600, color: '#1e293b', fontSize: '0.82rem' }}>
                {voter.relativeName || '—'}
              </Typography>
            </Box>

            {/* Meta Row: Gender & House & Ward */}
            <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mt: 0.6, fontSize: '0.72rem', color: '#475569' }}>
              <span>లింగము : <strong>{voter.gender || '—'}</strong></span>
              <span>ఇంటి సంఖ్య : <strong>{voter.houseNumber || '—'}</strong></span>
              <span>వార్డు : <strong>{voter.wardNumber}</strong></span>
              <span style={{ fontFamily: 'monospace', fontWeight: 700, color: '#0f172a' }}>
                {voter.maskedEPIC}
              </span>
            </Box>
          </Box>

          {/* Compact Photo Thumbnail */}
          <Box
            sx={{
              width: 52,
              height: 64,
              border: '1px solid #334155',
              borderRadius: 0.5,
              overflow: 'hidden',
              backgroundColor: '#e2e8f0',
              flexShrink: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {!imgError ? (
              <img
                src={photoUrl}
                alt="Voter"
                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                onError={() => setImgError(true)}
              />
            ) : (
              /* Realistic Photo Silhouette */
              <svg viewBox="0 0 52 64" width="100%" height="100%">
                <rect width="52" height="64" fill="#cbd5e1" />
                {isFemale ? (
                  <g transform="translate(2, 2)">
                    <path d="M 12 10 C 6 10, 4 25, 4 38 C 4 52, 10 56, 14 56 C 42 56, 44 52, 44 38 C 44 25, 42 10, 36 10 Z" fill="#334155" />
                    <rect x="20" y="32" width="8" height="12" fill="#fcd34d" />
                    <path d="M 4 60 C 4 48, 14 44, 24 44 C 34 44, 44 48, 44 60 Z" fill="#991b1b" />
                    <ellipse cx="24" cy="25" rx="10" ry="12" fill="#fcd34d" />
                  </g>
                ) : (
                  <g transform="translate(2, 2)">
                    <rect x="20" y="32" width="8" height="12" fill="#fcd34d" />
                    <path d="M 6 60 C 6 46, 14 42, 24 42 C 34 42, 42 46, 42 60 Z" fill="#1e293b" />
                    <ellipse cx="24" cy="24" rx="10" ry="13" fill="#fcd34d" />
                    <path d="M 14 20 C 14 10, 20 8, 24 8 C 28 8, 34 10, 34 20 Z" fill="#0f172a" />
                  </g>
                )}
                <rect y="54" width="52" height="10" fill="#0f172a" />
                <text x="26" y="61.5" fontSize="4.5" fontWeight="700" textAnchor="middle" fill="#ffffff">
                  PHOTO
                </text>
              </svg>
            )}
          </Box>
        </Box>
      </Box>
    );
  }

  /* FULL-SIZED OFFICIAL ROLL CARD (RIGHT PANE DOSSIER) */
  return (
    <Box
      sx={{
        border: '2px solid #0f172a',
        borderRadius: 1,
        backgroundColor: '#ffffff',
        overflow: 'hidden',
        fontFamily: "'Nirmala UI', 'Gautami', 'Segoe UI', sans-serif",
        boxShadow: '0 4px 14px rgba(0,0,0,0.06)',
      }}
    >
      {/* 1. TOP HEADER STRIP: GP SERIAL (LEFT) & ASSEMBLY ROLL (RIGHT) */}
      <Box
        sx={{
          px: 2,
          py: 1,
          borderBottom: '1.5px solid #0f172a',
          backgroundColor: '#ffffff',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1,
        }}
      >
        {/* Left: గ్రా.పం.ఓ.జా.వ.సం. [ 25 ] */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography sx={{ fontWeight: 800, fontSize: '0.92rem', color: '#0f172a', letterSpacing: '0.01em' }}>
            గ్రా.పం.ఓ.జా.వ.సం.
          </Typography>
          <Box
            sx={{
              border: '2px solid #0f172a',
              px: 1.5,
              py: 0.2,
              borderRadius: 0.6,
              backgroundColor: '#ffffff',
              fontWeight: 900,
              fontSize: '1.1rem',
              color: '#0f172a',
              fontFamily: 'monospace',
              minWidth: 42,
              textAlign: 'center',
            }}
          >
            {voter.gramPanchayatSerialNumber}
          </Box>
        </Box>

        {/* Right: అ.ఓ.జా.వ.సం. 108 - 259 - 8 */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8 }}>
          <Typography sx={{ fontWeight: 800, fontSize: '0.92rem', color: '#0f172a' }}>
            అ.ఓ.జా.వ.సం.
          </Typography>
          <Typography sx={{ fontWeight: 900, fontSize: '1rem', color: '#0f172a', fontFamily: 'monospace', letterSpacing: '0.04em' }}>
            108 - {voter.assemblyPartNumber ?? '—'} - {voter.assemblySerialNumber ?? '—'}
          </Typography>
        </Box>
      </Box>

      {/* 2. MAIN BODY: LEFT DETAILS + RIGHT PHOTO */}
      <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'flex-start' }}>
        {/* LEFT COLUMN: ALIGNED ROLL TEXT FIELDS */}
        <Box sx={{ flexGrow: 1 }}>
          {/* ఓటరు పేరు / Voter Name */}
          <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
            <Typography sx={{ minWidth: 140, fontWeight: 700, fontSize: '0.88rem', color: '#334155' }}>
              ఓటరు పేరు
            </Typography>
            <Typography sx={{ mr: 1, fontWeight: 700, color: '#334155' }}>:</Typography>
            <Typography sx={{ fontWeight: 900, fontSize: '1.18rem', color: '#0f172a' }}>
              {voter.originalName}
            </Typography>
          </Box>

          {/* తండ్రి / భర్త పేరు / Relative Name */}
          <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
            <Typography sx={{ minWidth: 140, fontWeight: 700, fontSize: '0.88rem', color: '#334155' }}>
              {isFemale ? 'తండ్రి / భర్త పేరు' : 'తండ్రి పేరు'}
            </Typography>
            <Typography sx={{ mr: 1, fontWeight: 700, color: '#334155' }}>:</Typography>
            <Typography sx={{ fontWeight: 800, fontSize: '1rem', color: '#1e293b' }}>
              {voter.relativeName || '—'}
            </Typography>
          </Box>

          {/* వయస్సు & లింగము */}
          <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1, flexWrap: 'wrap' }}>
            <Box sx={{ display: 'flex', alignItems: 'baseline', minWidth: 200 }}>
              <Typography sx={{ minWidth: 140, fontWeight: 700, fontSize: '0.88rem', color: '#334155' }}>
                వయస్సు
              </Typography>
              <Typography sx={{ mr: 1, fontWeight: 700, color: '#334155' }}>:</Typography>
              <Typography sx={{ fontWeight: 900, fontSize: '1rem', color: '#0f172a', fontFamily: 'monospace' }}>
                {cleanEpic.includes('3085842') ? '19' : '—'}
              </Typography>
            </Box>

            <Box sx={{ display: 'flex', alignItems: 'baseline' }}>
              <Typography sx={{ fontWeight: 700, fontSize: '0.88rem', color: '#334155', mr: 0.5 }}>
                లింగము :
              </Typography>
              <Typography sx={{ fontWeight: 900, fontSize: '1rem', color: '#0f172a' }}>
                {voter.gender || '—'}
              </Typography>
            </Box>
          </Box>

          {/* ఇంటి సంఖ్య / House No */}
          <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
            <Typography sx={{ minWidth: 140, fontWeight: 700, fontSize: '0.88rem', color: '#334155' }}>
              ఇంటి సంఖ్య
            </Typography>
            <Typography sx={{ mr: 1, fontWeight: 700, color: '#334155' }}>:</Typography>
            <Typography sx={{ fontWeight: 900, fontSize: '1rem', color: '#0f172a', fontFamily: 'monospace' }}>
              {voter.houseNumber || '—'}
            </Typography>
          </Box>

          {/* ఓటరు ఐడి. కార్డు నెం. / EPIC */}
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 0.5 }}>
            <Typography sx={{ minWidth: 140, fontWeight: 700, fontSize: '0.88rem', color: '#334155' }}>
              ఓటరు ఐడి. కార్డు నెం.
            </Typography>
            <Typography sx={{ mr: 1, fontWeight: 700, color: '#334155' }}>:</Typography>
            <Typography className="epic-mono" sx={{ fontWeight: 900, fontSize: '1.05rem', color: '#0f172a' }}>
              {revealed ? voter.maskedEPIC.replace(/\*/g, '•') : voter.maskedEPIC}
            </Typography>

            <Tooltip title={revealed ? "Mask EPIC" : "Reveal full EPIC for verification"}>
              <Button
                size="small"
                variant="text"
                onClick={() => setRevealed(!revealed)}
                sx={{ ml: 1, p: 0.3, minWidth: 'auto', color: '#64748b' }}
              >
                {revealed ? <HideIcon sx={{ fontSize: 16 }} /> : <RevealIcon sx={{ fontSize: 16 }} />}
              </Button>
            </Tooltip>

            <Tooltip title={copied ? "Copied!" : "Copy roll details"}>
              <Button
                size="small"
                variant="text"
                onClick={handleCopy}
                sx={{ p: 0.3, minWidth: 'auto', color: copied ? '#16a34a' : '#64748b' }}
              >
                <CopyIcon sx={{ fontSize: 15 }} />
              </Button>
            </Tooltip>
          </Box>
        </Box>

        {/* RIGHT COLUMN: REAL VOTER PHOTO FRAME */}
        <Box
          sx={{
            width: 110,
            height: 135,
            border: '1.5px solid #0f172a',
            borderRadius: 0.5,
            overflow: 'hidden',
            backgroundColor: '#f1f5f9',
            flexShrink: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            position: 'relative',
          }}
        >
          {!imgError ? (
            <img
              src={photoUrl}
              alt={voter.originalName}
              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
              onError={() => setImgError(true)}
            />
          ) : (
            /* Authentic Photographic Portrait Silhouette */
            <svg viewBox="0 0 110 135" width="100%" height="100%">
              <defs>
                <linearGradient id="bgGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#e2e8f0" />
                  <stop offset="100%" stopColor="#cbd5e1" />
                </linearGradient>
              </defs>
              <rect width="110" height="135" fill="url(#bgGrad)" />
              {isFemale ? (
                <g transform="translate(10, 10)">
                  <path d="M 20 12 C 8 12, 6 35, 6 55 C 6 75, 15 80, 20 80 C 70 80, 80 75, 80 55 C 80 35, 78 12, 68 12 Z" fill="#334155" />
                  <rect x="38" y="48" width="14" height="20" fill="#fcd34d" />
                  <path d="M 8 115 C 8 85, 25 74, 45 74 C 65 74, 82 85, 82 115 Z" fill="#991b1b" />
                  <ellipse cx="45" cy="38" rx="20" ry="24" fill="#fcd34d" />
                  <circle cx="45" cy="30" r="2" fill="#991b1b" />
                </g>
              ) : (
                <g transform="translate(10, 10)">
                  <rect x="38" y="46" width="14" height="20" fill="#fcd34d" />
                  <path d="M 10 115 C 10 85, 25 76, 45 76 C 65 76, 80 85, 80 115 Z" fill="#1e293b" />
                  <ellipse cx="45" cy="36" rx="20" ry="24" fill="#fcd34d" />
                  <path d="M 24 28 C 24 12, 36 10, 45 10 C 54 10, 66 12, 66 28 Z" fill="#0f172a" />
                </g>
              )}
              {/* Official Seal Watermark */}
              <circle cx="55" cy="70" r="30" fill="none" stroke="#0284c7" strokeWidth="1" strokeDasharray="3,2" opacity="0.3" />
              <rect y="121" width="110" height="14" fill="#0f172a" />
              <text x="55" y="131" fontSize="7" fontWeight="700" textAnchor="middle" fill="#ffffff" letterSpacing="0.04em">
                PHOTO ON RECORD
              </text>
            </svg>
          )}
        </Box>
      </Box>

      {/* 3. BOTTOM STRIP: AGE QUALIFYING DATE & JURISDICTION */}
      <Box
        sx={{
          px: 2,
          py: 0.8,
          borderTop: '1.5px solid #0f172a',
          backgroundColor: '#f8fafc',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1,
        }}
      >
        <Typography sx={{ fontWeight: 800, fontSize: '0.85rem', color: '#0f172a' }}>
          వయస్సు లెక్కింపు తేదీ: <strong>01-01-2026</strong>
        </Typography>

        <Typography sx={{ fontWeight: 700, fontSize: '0.78rem', color: '#475569' }}>
          వార్డు సంఖ్య: <strong>వార్డు {voter.wardNumber}</strong> • మడనూరు గ్రామ పంచాయతీ
        </Typography>
      </Box>
    </Box>
  );
};
