# Madanuru Voter Seva - Source PDF Inspection Report

**Date of Inspection**: 2026-11-13  
**Source File**: `MADANURU WITH SORTING VOTER.pdf`  
**File Size**: 437.02 MB (458,249,570 bytes)  
**Location**: `MadanuruVoterSeva/data/source/MADANURU WITH SORTING VOTER.pdf`  
**Inspected Using**: PdfPig (UglyToad.PdfPig 0.1.16) layout & text stream analyzer

---

## 1. Executive Summary & Verification of Roll Expectations

| Metric | Plan Expectation | Actual Inspected Source Value | Verification Status |
| :--- | :--- | :--- | :--- |
| **Total Pages** | 214 pages | **214 pages** | Verified Match |
| **Total Voters** | ~5,543 voters | **5,543 voters** | Verified Exact Match |
| **Total Wards** | 14 wards | **14 wards (Ward 1 to 14)** | Verified Exact Match |
| **Male Voters** | Not specified in plan | **2,669** | Verified Source (Page 2) |
| **Female Voters** | Not specified in plan | **2,874** | Verified Source (Page 2) |
| **Third Gender / Others**| Not specified in plan | **0** | Verified Source (Page 2) |
| **Constituency** | 108 - Ongole | **108-ఒంగోలు (Ongole)** | Verified Source (Page 1) |
| **Mandal** | Kothapatnam | **కొత్తపట్నం (Kothapatnam)** | Verified Source (Page 1) |
| **Gram Panchayat** | Madanuru | **మడనూరు (Madanuru)** | Verified Source (Page 1) |
| **District** | Prakasam | **ప్రకాశం (Prakasam)** | Verified Source (Page 1) |
| **Assembly Polling Stations** | Not specified | **254, 255, 256, 257, 258, 259** | Verified Source (Page 1) |

---

## 2. Document Structure Breakdown

### Page 1: Electoral Roll Cover / Title Page (`ముఖపత్రం - 2026`)
- **Authority**: ఓటరు జాబితా ప్రచురణాధికారి & జిల్లా పంచాయతీ అధికారి, ప్రకాశం
- **Gram Panchayat**: మడనూరు గ్రామ పంచాయతీ సాధారణ ఎన్నికలు - 2026
- **District**: ప్రకాశం
- **Division**: ఒంగోలు
- **Assembly Constituency**: 108 - ఒంగోలు
- **Mandal**: కొత్తపట్నం
- **Habitation / Village Area**: 1. మడనూరు (House numbers: 1-1 to Madanuru, Voters: 1 to 5543)
- **Assembly Polling Stations Listed**: 254, 255, 256, 257, 258, 259
- **Source Platform**: SEC ANDHRA PRADESH (`sec-ems.ap.gov.in`)

### Page 2: Official Ward-Wise Voter Distribution Table
The roll provides an exact official ward-by-ward breakdown:

| Ward No. | GP Serial Range | Male | Female | Other | Total Voters |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | 1 – 395 | 186 | 209 | 0 | 395 |
| **2** | 396 – 790 | 185 | 210 | 0 | 395 |
| **3** | 791 – 1185 | 189 | 206 | 0 | 395 |
| **4** | 1186 – 1580 | 190 | 205 | 0 | 395 |
| **5** | 1581 – 1975 | 200 | 195 | 0 | 395 |
| **6** | 1976 – 2370 | 188 | 207 | 0 | 395 |
| **7** | 2371 – 2765 | 201 | 194 | 0 | 395 |
| **8** | 2766 – 3160 | 181 | 214 | 0 | 395 |
| **9** | 3161 – 3555 | 188 | 207 | 0 | 395 |
| **10** | 3556 – 3950 | 187 | 208 | 0 | 395 |
| **11** | 3951 – 4345 | 193 | 202 | 0 | 395 |
| **12** | 4346 – 4740 | 197 | 198 | 0 | 395 |
| **13** | 4741 – 5135 | 181 | 214 | 0 | 395 |
| **14** | 5136 – 5543 | 203 | 205 | 0 | 408 |
| **TOTAL** | **1 – 5543** | **2,669** | **2,874** | **0** | **5,543** |

### Pages 3 to 214: Voter Detail Pages
- **Format**: 3 columns x ~9-10 rows (~27-30 voter cards per page).
- **Text Layer Quality**: Digital vector font stream. Not a flat scan. Characters extract cleanly via PdfPig without requiring full-page OCR.
- **Card Fields Identified**:
  1. `గా.పం.ఓ.జా.వ.సం.` -> **Gram Panchayat Electoral Roll Serial Number** (e.g., `1`, `28`, `55`)
  2. `అ.ఓ.జా.వ.సం.` -> **Assembly Roll Reference** in format `[Assembly Constituency] - [Part/Station] - [Serial]` (e.g., `108 - 255 - 1`)
  3. `ఓటరు పేరు:` -> **Voter Name** in original Telugu script (e.g., `శేష రెడ్డి గండవరపు`)
  4. `తండ్రి పేరు:` / `భర్త పేరు:` -> **Relation and Relative Name** (Father or Husband in Telugu)
  5. `వయసు :` -> **Age** (e.g., `74`)
  6. `లింగము :` -> **Gender** (`పు` = Male, `స్త్రీ` = Female)
  7. `ఇంటి సంఖ్య :` -> **House Number** (e.g., `1-1`, `1-3`)
  8. `ఓటరు ఐడి. కార్డు నెం. :` -> **EPIC Identifier** (e.g., `UPD2751808`, `UPD2425908`, `UPD3086881`)

---

## 3. Extraction & Staging Recommendations for Phase 3

1. **Dual Serial Separation**:
   - Store `GramPanchayatSerialNumber` (sequential 1 to 5543) as the primary GP roll identifier.
   - Store `AssemblyPartNumber` and `AssemblySerialNumber` in distinct fields; never combine or confuse them with the GP serial.
2. **EPIC Normalization**:
   - Normalize EPIC by trimming whitespace, removing internal spaces/dashes, and storing uppercase.
   - Index `NormalizedEPIC` for instantaneous O(1) indexed lookup.
3. **Telugu Script Preservation**:
   - Store all Telugu names (`OriginalName`, `RelativeName`) in UTF-8 without alteration or phonetic invention.
   - Support `VerifiedEnglishName` and `VerifiedEnglishRelativeName` as distinct, verified fields.
4. **Ward Boundaries**:
   - Ward numbers change on specific pages corresponding to the table on Page 2. The parser will check both the page header (`వార్డు నెంబరు : X`) and verify consistency against the serial range table.
5. **Targeted OCR Handling**:
   - Digital text extraction succeeded on all tested sample pages with >4000 characters per page. OCR will only be engaged if a page yields zero glyphs or corrupted font encodings.
