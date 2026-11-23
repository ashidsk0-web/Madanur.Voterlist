import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Autocomplete, Box, Button, Card, MenuItem, Pagination, Stack, TextField, Typography } from '@mui/material';
import { BadgeOutlined, DescriptionOutlined, HowToVoteOutlined, Search } from '@mui/icons-material';
import axios from 'axios';
import { api } from '../api/client';
import type { VoterDetail, VoterSearchResult, WardSummary } from '../types';
import './lookup.css';

const pageSize = 12;
type Query = { method: 'epic' | 'name'; value: string; ward: number };

function SuggestionPhoto({ voterId }: { voterId: number }) {
  const [source, setSource] = useState<string>();

  useEffect(() => {
    const controller = new AbortController();
    let url: string | undefined;

    api.getVoterPhoto(voterId, controller.signal).then(photo => {
      if (controller.signal.aborted) return;
      url = URL.createObjectURL(photo);
      setSource(url);
    }).catch(() => {});

    return () => {
      controller.abort();
      if (url) URL.revokeObjectURL(url);
    };
  }, [voterId]);

  return source
    ? <img className="lookup-suggestion-photo" src={source} alt="" />
    : <span className="lookup-suggestion-photo" aria-hidden="true"><BadgeOutlined fontSize="small" /></span>;
}

export function VoterSearchPage() {
  const [method, setMethod] = useState<Query['method']>('epic');
  const [epic, setEpic] = useState('');
  const [name, setName] = useState('');
  const [suggestions, setSuggestions] = useState<VoterSearchResult[]>([]);
  const [suggesting, setSuggesting] = useState(false);
  const [suggestionBusy, setSuggestionBusy] = useState(false);
  const [ward, setWard] = useState(0);
  const [wards, setWards] = useState<WardSummary[]>([]);
  const [query, setQuery] = useState<Query>();
  const [matches, setMatches] = useState<VoterSearchResult[]>([]);
  const [selected, setSelected] = useState<VoterSearchResult>();
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const [sourceError, setSourceError] = useState('');
  const [record, setRecord] = useState<{ id: number; detail?: VoterDetail; photo?: string; error?: string }>();
  const selectedId = selected?.voterId;
  const currentRecord = record?.id === selectedId ? record : undefined;
  const detail = currentRecord?.detail;
  const voter = detail ?? selected;

  useEffect(() => {
    const term = name.trim();
    if (method !== 'name' || !suggesting || !term || busy) return;
    const controller = new AbortController();
    const timer = window.setTimeout(async () => {
      setSuggestionBusy(true);
      try {
        const result = await api.searchByName(term, ward, 1, 8, controller.signal);
        if (!controller.signal.aborted) setSuggestions(result.items);
      } catch {
        if (!controller.signal.aborted) setSuggestions([]);
      } finally {
        if (!controller.signal.aborted) setSuggestionBusy(false);
      }
    }, 250);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [name, ward, method, suggesting, busy]);

  useEffect(() => {
    if (selectedId === undefined) return;
    const id = selectedId;
    const controller = new AbortController();
    let photoUrl: string | undefined;

    async function loadRecord() {
      try {
        const detail = await api.getVoterById(id, controller.signal);
        if (controller.signal.aborted) return;
        setRecord({ id, detail });
        if (!detail.hasPhoto) return;
        const image = await api.getVoterPhoto(id, controller.signal);
        if (controller.signal.aborted) return;
        photoUrl = URL.createObjectURL(image);
        setRecord({ id, detail, photo: photoUrl });
      } catch {
        if (controller.signal.aborted) return;
        setRecord(previous => previous?.id === id && previous.detail
          ? { ...previous, error: 'Could not load the stored photo.' }
          : { id, error: 'Could not load the complete roll entry.' });
      }
    }
    void loadRecord();
    return () => {
      controller.abort();
      if (photoUrl) URL.revokeObjectURL(photoUrl);
    };
  }, [selectedId]);

  useEffect(() => {
    if (selectedId === undefined || !window.matchMedia('(max-width: 600px)').matches) return;
    const frame = window.requestAnimationFrame(() => {
      document.querySelector('.lookup-record')?.scrollIntoView({ block: 'start' });
    });
    return () => window.cancelAnimationFrame(frame);
  }, [selectedId]);

  function closeSuggestions() {
    setSuggesting(false);
    setSuggestions([]);
    setSuggestionBusy(false);
  }

  useEffect(() => {
    api.getWards().then(setWards).catch(() => setNotice('Could not load wards. Check the local API connection.'));
  }, []);

  useEffect(() => {
    if (!selected) return;
    const timer = window.setTimeout(() => {
      setSelected(undefined);
      setRecord(undefined);
      closeSuggestions();
      setMatches([]);
      setQuery(undefined);
      setTotal(0);
      setEpic('');
      setName('');
      setWard(0);
      setSourceError('');
    }, 120_000);
    return () => window.clearTimeout(timer);
  }, [selected]);

  function clear() {
    closeSuggestions();
    setRecord(undefined);
    setEpic('');
    setName('');
    setWard(0);
    setQuery(undefined);
    setMatches([]);
    setSelected(undefined);
    setTotal(0);
    setPage(1);
    setNotice('');
    setSourceError('');
  }

  async function lookup(next: Query, nextPage = 1) {
    closeSuggestions();
    setRecord(undefined);
    setBusy(true);
    setQuery(next);
    setPage(nextPage);
    setNotice('');
    setSourceError('');
    setMatches([]);
    setSelected(undefined);
    setTotal(0);
    try {
      if (next.method === 'epic') {
        setSelected(await api.searchByEpic(next.value));
      } else {
        const response = await api.searchByName(next.value, next.ward, nextPage, pageSize);
        setMatches(response.items);
        setTotal(response.totalCount);
        if (response.totalCount === 1) setSelected(response.items[0]);
        if (response.totalCount === 0) setNotice('No matching entry in the local roll. Check the spelling or ask the election official to verify the roll.');
      }
    } catch (error) {
      setNotice(axios.isAxiosError(error) && error.response?.status === 404
        ? 'No matching EPIC in the local roll. Check the number or ask the election official to verify the roll.'
        : 'Lookup failed. Check the local API connection and try again.');
    } finally {
      setBusy(false);
    }
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    const value = (method === 'epic' ? epic : name).trim();
    if (value || (method === 'name' && ward)) void lookup({ method, value, ward });
  }

  async function openSource() {
    if (!selected) return;
    const tab = window.open('about:blank', '_blank');
    if (tab) tab.opener = null;
    setSourceError('');
    try {
      const { url } = await api.getVoterSourceLink(selected.voterId);
      const target = url + '#page=' + selected.sourcePageNumber;
      if (tab) tab.location.replace(target);
      else window.location.assign(target);
    } catch {
      tab?.close();
      setSourceError('Could not open the source PDF. Use the page number shown on this record.');
    }
  }

  return (
    <div className="lookup">
      <div className="lookup-heading">
        <span className="lookup-heading-icon"><HowToVoteOutlined aria-hidden="true" /></span>
        <div>
          <h1>Voter lookup</h1>
          <p lang="te">ఓటరు వివరాల శోధన</p>
        </div>
      </div>
      <Card className="lookup-panel lookup-search" variant="outlined">
        <Box component="form" className="lookup-form" onSubmit={submit}>
            <div className="lookup-tabs" aria-label="Search method">
              <button type="button" aria-pressed={method === 'epic'} disabled={busy} onClick={() => { clear(); setMethod('epic'); }}>
                <BadgeOutlined fontSize="small" /> EPIC ID
              </button>
              <button type="button" aria-pressed={method === 'name'} disabled={busy} onClick={() => { clear(); setMethod('name'); }}>
                <Search fontSize="small" /> పేరు / Name
              </button>
            </div>
              {method === 'epic' ? (
                <TextField key="epic" className="lookup-fields" fullWidth label="ఓటరు ఐడి / EPIC number"
                  placeholder="ABC1234567" value={epic} autoComplete="off" disabled={busy}
                  slotProps={{ htmlInput: { maxLength: 40, spellCheck: false } }}
                  onChange={event => setEpic(event.target.value.toUpperCase())} />
              ) : (
                <div className="lookup-fields lookup-name-fields">
                  <Autocomplete<VoterSearchResult, false, false, true>
                    freeSolo fullWidth disabled={busy}
                    slotProps={{ listbox: { className: 'lookup-suggestion-list' } }} options={suggestions} inputValue={name}
                    loading={suggestionBusy} filterOptions={options => options}
                    getOptionKey={option => typeof option === 'string' ? option : option.voterId}
                    getOptionLabel={option => typeof option === 'string' ? option : option.originalName}
                    onInputChange={(_, value, reason) => {
                      if (reason !== 'input' && reason !== 'clear') return;
                      setName(value);
                      setSuggestions([]);
                      setSuggestionBusy(false);
                      setSuggesting(true);
                    }}
                    onChange={(event, option) => {
                      if (typeof option === 'string') {
                        if (event.defaultPrevented && option.trim()) void lookup({ method: 'name', value: option.trim(), ward });
                        return;
                      }
                      if (!option) return;
                      closeSuggestions();
                      setName(option.originalName);
                      setMatches([]);
                      setQuery(undefined);
                      setTotal(0);
                      setNotice('');
                      setSourceError('');
                      setSelected(option);
                    }}
                    renderOption={(props, option) => {
                      const { key, ...optionProps } = props;
                      return <li key={key} {...optionProps} className={optionProps.className + ' lookup-suggestion'}>
                        <SuggestionPhoto key={option.voterId} voterId={option.voterId} />
                        <div><strong lang="te">{option.originalName}</strong>
                          <Typography variant="body2" color="text.secondary">
                            Ward {option.wardNumber} · #{option.gramPanchayatSerialNumber}
                            {option.relativeName && <> · <span lang="te">{option.relativeName}</span></>}
                          </Typography>
                        </div>
                      </li>;
                    }}
                    renderInput={params => <TextField {...params} label="ఓటరు పేరు / Name"
                      placeholder="Type a name in Telugu or English"
                      slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput, maxLength: 100 } }} />}
                  />
                  <TextField select fullWidth label="వార్డు / Ward" value={ward} disabled={busy}
                    onChange={event => {
                      setSuggestions([]);
                      setSuggestionBusy(false);
                      setSuggesting(true);
                      setWard(Number(event.target.value));
                    }}>
                    <MenuItem value={0}>All wards</MenuItem>
                    {wards.map(item => <MenuItem key={item.wardId} value={item.wardNumber}>Ward {item.wardNumber}</MenuItem>)}
                  </TextField>
                </div>
              )}
              <div className="lookup-actions">
                <Button type="submit" variant="contained" size="large" startIcon={<Search />}
                  disabled={busy || (method === 'epic' ? !epic.trim() : !name.trim() && !ward)}>
                  {busy ? 'Searching…' : 'Search'}
                </Button>
                <Button onClick={clear} disabled={busy}>Clear</Button>
              </div>
        </Box>
      </Card>
      {notice && <Alert className="lookup-notice" severity="warning">{notice}</Alert>}
      <div className={matches.length && voter ? 'lookup-grid lookup-grid-split' : 'lookup-grid'}>
        {matches.length > 0 && (
            <Card className="lookup-panel" variant="outlined">
              <Typography component="h2" variant="h6">{total.toLocaleString('en-IN')} matching entries</Typography>
              <Typography color="text.secondary" sx={{ mb: 1.5 }}>Confirm the name and ward with the voter.</Typography>
              <Stack spacing={1}>
                {matches.map(voter => (
                  <button className="lookup-match" key={voter.voterId} type="button"
                    aria-pressed={selected?.voterId === voter.voterId} onClick={() => { setSelected(voter); setSourceError(''); }}>
                    <span className="lookup-match-name"><strong lang="te">{voter.originalName || 'Check PDF for name'}</strong>
                      {voter.relativeName && <small lang="te">{voter.relativeName}</small>}</span>
                    <span>Ward {voter.wardNumber}<small>#{voter.gramPanchayatSerialNumber}</small></span>
                  </button>
                ))}
              </Stack>
              {total > pageSize && query && <Pagination className="desk-pagination" siblingCount={0} page={page} count={Math.ceil(total / pageSize)}
                onChange={(_, value) => void lookup(query, value)} sx={{ mt: 2 }} />}
            </Card>
          )}
        <Card className="lookup-panel lookup-record" variant="outlined" aria-live="polite">
          {!voter ? (
            <div className="lookup-empty">
              <DescriptionOutlined />
              <h2>Voter roll entry</h2>
              <p>{matches.length ? 'Select a match to view the voter record.' : 'Search by EPIC ID or by name in Telugu or English.'}</p>
            </div>
          ) : (
            <>
              <div className="lookup-identity">
                {currentRecord?.photo
                  ? <img className="lookup-photo" src={currentRecord.photo} alt="Voter photograph from the electoral roll" />
                  : <div className="lookup-photo lookup-photo-status">
                      {currentRecord?.error ? 'Photo unavailable' : detail && !detail.hasPhoto ? 'No photo on file' : 'Loading photo…'}
                    </div>}
                <div className="lookup-person">
                  <p className="lookup-record-label">ఓటరు వివరాలు / Roll entry</p>
                  <h2 lang="te">{voter.originalName || 'Check PDF for name'}</h2>
                  {voter.verifiedEnglishName && <Typography>{voter.verifiedEnglishName}</Typography>}
                  {voter.relativeName && <Typography className="lookup-relative">
                    {detail?.relativeLabel || 'Relative'} {' / '}<span lang="te">{voter.relativeName}</span>
                    {voter.verifiedEnglishRelativeName && <> ({voter.verifiedEnglishRelativeName})</>}
                  </Typography>}
                  <dl className="lookup-refs">
                    <div><dt>Ward</dt><dd>{voter.wardNumber}</dd></div>
                    <div><dt>GP serial</dt><dd>{voter.gramPanchayatSerialNumber}</dd></div>
                    <div><dt>PDF page</dt><dd>{voter.sourcePageNumber}</dd></div>
                  </dl>
                </div>
              </div>
              <div className="lookup-record-body">
              {detail && <dl className="lookup-details">
                <div><dt>EPIC ID</dt><dd>{detail.epicNumber}</dd></div>
                <div><dt>Age on roll</dt><dd>{detail.age ?? 'Unavailable'}</dd></div>
                <div><dt>Gender</dt><dd lang="te">{detail.gender || 'Unavailable'}</dd></div>
                <div><dt>House number</dt><dd>{detail.houseNumber || 'Unavailable'}</dd></div>
                <div><dt>Assembly constituency</dt><dd>{detail.assemblyConstituencyNumber ?? 'Unavailable'}</dd></div>
                <div><dt>Assembly part</dt><dd>{detail.assemblyPartNumber ?? 'Unavailable'}</dd></div>
                <div><dt>Assembly serial</dt><dd>{detail.assemblySerialNumber ?? 'Unavailable'}</dd></div>
                <div><dt>Village</dt><dd>{detail.rollVillageName}</dd></div>
                {detail.rollPublicationDate && <div><dt>Roll publication</dt><dd>{detail.rollPublicationDate}</dd></div>}
              </dl>}
              <Stack spacing={1.5}>
                {voter.verificationStatus === 'NameReview' &&
                  <Alert severity="warning">Check the name against the PDF. Some Telugu characters were unreadable in extraction.</Alert>}
                <Alert severity={voter.pollingStationVerificationStatus === 'Verified' ? 'success' : 'info'}>
                  {voter.pollingStationVerificationStatus === 'Verified' ? (
                    <><strong>{voter.pollingStationName}</strong>{voter.pollingStationAddress && <div>{voter.pollingStationAddress}</div>}</>
                  ) : 'Polling station assignment unavailable. Confirm it with the election office.'}
                </Alert>
                {currentRecord?.error && <Alert severity="error">{currentRecord.error}</Alert>}
                {detail?.rawCardText && <details className="lookup-source-text" key={detail.voterId}>
                  <summary>Extracted source card text</summary>
                  <pre lang="te">{detail.rawCardText}</pre>
                </details>}
                {sourceError && <Alert severity="error">{sourceError}</Alert>}
                <div className="lookup-source-action">
                  <Button variant="outlined" startIcon={<DescriptionOutlined />} onClick={() => void openSource()}>Open source PDF</Button>
                </div>
              </Stack>
              </div>
            </>
          )}
        </Card>
      </div>
    </div>
  );
}
