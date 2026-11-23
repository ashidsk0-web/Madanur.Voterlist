# Madanuru Voter Seva - Architecture & Implementation Guide

## 1. Project Directory Structure

```
MadanuruVoterSeva/
├── frontend/                          # React + Vite + TypeScript Single Page Application
│   ├── src/
│   │   ├── api/                       # Axios API client & endpoints
│   │   ├── components/                # Reusable UI components (Navbar, MaskedEPIC, etc.)
│   │   ├── layouts/                   # Main desktop/laptop layout
│   │   ├── pages/                     # Dashboard, VoterSearch, VoterDetail, WardDirectory, Admin
│   │   ├── routes/                    # React Router definitions
│   │   ├── hooks/                     # Custom React hooks (debounce, keyboard, auth)
│   │   ├── types/                     # TypeScript DTO & entity interfaces
│   │   ├── utils/                     # Formatters, EPIC normalizer, local helpers
│   │   └── theme/                     # Enterprise light Material UI theme
│   ├── index.html
│   ├── package.json
│   └── vite.config.ts
├── backend/                           # ASP.NET Core Web API (.NET 10)
│   ├── Controllers/                   # REST API controllers
│   ├── Services/                      # PDF parser, staging, voter search, auth services
│   ├── Interfaces/                    # Service and repository contracts
│   ├── Repositories/                  # Dapper parameterized SQL repositories
│   ├── Models/                        # Domain entities
│   ├── DTOs/                          # Request & response data transfer objects
│   ├── Data/                          # SQLite factory, DbInitializer, PDF inspector
│   ├── Middleware/                    # Exception handling, security headers
│   ├── appsettings.json
│   └── MadanuruVoterSeva.Backend.csproj
├── data/
│   ├── source/                        # Read-only original electoral roll PDF
│   │   └── MADANURU WITH SORTING VOTER.pdf
│   ├── database/                      # SQLite database files & schema
│   │   ├── madanuru_voter_seva.db
│   │   └── schema.sql
│   └── backups/                       # Local offline automated database backups
└── documentation/                     # Technical, operational, and test documentation
    ├── PDF_INSPECTION_REPORT.md
    └── PROJECT_ARCHITECTURE.md
```

## 2. Technology Stack & Operational Guardrails

- **Frontend**: React 19, TypeScript, Material UI (MUI v6), React Router v7, Axios, React Hook Form.
- **Backend**: ASP.NET Core Web API, C# (.NET 10), Dapper, Microsoft.Data.Sqlite, UglyToad.PdfPig.
- **Database**: SQLite with Write-Ahead Logging (WAL) and enforced foreign keys.
- **Offline Guarantee**: Fully self-contained local binaries, bundled fonts and assets. Zero CDN dependencies, zero telemetry, zero cloud calls.
- **Security & Privacy**:
  - Masked EPIC display (e.g., `UPD*****46`) on search results.
  - No search history or voter query retention in database or audit logs.
  - Role-based separation: Desk lookup vs. Admin roll management.
