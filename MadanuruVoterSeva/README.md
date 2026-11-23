# Madanuru Voter Seva

Run **start-backend.bat** from the parent folder and open http://localhost:5000. The backend serves the built frontend and uses the SQLite database in this project's data/database folder. Internet access is unnecessary at the lookup desk.

The application accepts a single administrator account named admin. To change
its password, run the following from this project's backend directory:

```powershell
$credential = Get-Credential -UserName admin -Message 'Enter the new admin password'
$credential.GetNetworkCredential().Password | dotnet run -- configure-admin
Remove-Variable credential
```

Restart the backend after changing the password to invalidate existing sessions.

## Desk workflow

1. Search by exact EPIC. If the voter does not have it, enter a partial Telugu name and optionally select a ward.
2. For multiple matches, confirm the name, relative's name, and ward with the voter before opening a result.
3. Read back the Gram Panchayat serial and ward. Open the linked source page when a name requires review.
4. Clear the result before assisting the next voter. A selected result also clears after two minutes.

## Roll and polling-station status

The included PDF has 214 pages. All 5,543 active EPIC references were located on their corresponding PDF pages. The ward totals are 395 for each of wards 1 through 13 and 408 for ward 14. Search normalizes spacing and strips unreadable control or replacement characters. Missing Telugu glyphs still require review against the PDF.

The supplied database has no verified polling-station assignments. Confirm the current election roll, supplements, and station assignments with the local election office before desk rollout. The application displays a station only when both the station and its voter assignment are marked verified for the active roll.

Back up the SQLite database before changing the active roll. The importer is configured for the supplied Madanuru 2026 PDF and its ward totals. Stage and validate an import in the admin screen, then activate the validated version. For a revised roll with different counts or pagination, check its structure and update the import expectations before running the import.

