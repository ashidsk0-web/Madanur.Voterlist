export interface DashboardStatistics {
  totalImportedVoters: number;
  totalWards: number;
  activeRollVersion: string;
  activeRollVersionId: number | null;
  lastImportDate: string | null;
  recordsPendingVerification: number;
  importValidationStatus: string;
  stagedRecordsCount: number;
  validStagedRecordsCount: number;
  warningStagedRecordsCount: number;
  errorStagedRecordsCount: number;
}

export interface VoterSearchResult {
  voterId: number;
  rollVersionId: number;
  maskedEPIC: string;
  originalName: string;
  verifiedEnglishName?: string;
  relativeName?: string;
  verifiedEnglishRelativeName?: string;
  gender?: string;
  wardNumber: number;
  wardName?: string;
  gramPanchayatSerialNumber: number;
  assemblyPartNumber?: number;
  assemblySerialNumber?: number;
  houseNumber?: string;
  sourcePageNumber: number;
  verificationStatus: string;
  pollingStationName?: string;
  pollingStationAddress?: string;
  pollingStationVerificationStatus: string;
}

export interface VoterDetail extends VoterSearchResult {
  epicNumber: string;
  age: number | null;
  relativeLabel: string | null;
  assemblyConstituencyNumber: number | null;
  rawCardText: string | null;
  hasPhoto: boolean;
  rollElectionName: string;
  rollVillageName: string;
  rollPublicationDate?: string;
}

export interface WardSummary {
  wardId: number;
  wardNumber: number;
  wardName?: string;
  totalVoters: number;
  verifiedVoters: number;
  minSerialNumber: number;
  maxSerialNumber: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ImportPreview {
  fileName: string;
  fileSizeBytes: number;
  totalPages: number;
  expectedVoters: number;
  expectedWards: number;
  district: string;
  mandal: string;
  village: string;
  status: string;
}

export interface ImportValidationSummary {
  rollVersionId: number;
  totalExtracted: number;
  validRecords: number;
  warningsCount: number;
  errorsCount: number;
  canApprove: boolean;
  validationMessages: string[];
}

export interface UserSession {
  username: string;
  role: string;
  token?: string;
  isAuthenticated: boolean;
}
