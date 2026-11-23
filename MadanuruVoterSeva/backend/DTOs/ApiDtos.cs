namespace MadanuruVoterSeva.Backend.DTOs;

public class DashboardStatisticsDto
{
    public int TotalImportedVoters { get; set; }
    public int TotalWards { get; set; }
    public string ActiveRollVersion { get; set; } = "None";
    public int? ActiveRollVersionId { get; set; }
    public string? LastImportDate { get; set; }
    public int RecordsPendingVerification { get; set; }
    public string ImportValidationStatus { get; set; } = "Not Started";
    public int StagedRecordsCount { get; set; }
    public int ValidStagedRecordsCount { get; set; }
    public int WarningStagedRecordsCount { get; set; }
    public int ErrorStagedRecordsCount { get; set; }
}

public class VoterSearchResultDto
{
    public int VoterId { get; set; }
    public int RollVersionId { get; set; }
    public string MaskedEPIC { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? VerifiedEnglishName { get; set; }
    public string? RelativeName { get; set; }
    public string? VerifiedEnglishRelativeName { get; set; }
    public string? Gender { get; set; }
    public int WardNumber { get; set; }
    public string? WardName { get; set; }
    public int GramPanchayatSerialNumber { get; set; }
    public int? AssemblyPartNumber { get; set; }
    public int? AssemblySerialNumber { get; set; }
    public string? HouseNumber { get; set; }
    public int SourcePageNumber { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
    public string? PollingStationName { get; set; }
    public string? PollingStationAddress { get; set; }
    public string PollingStationVerificationStatus { get; set; } = "Unverified";
}

public class VoterDetailDto : VoterSearchResultDto
{
    public string EpicNumber { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? RelativeLabel { get; set; }
    public int? AssemblyConstituencyNumber { get; set; }
    public string? RawCardText { get; set; }
    public bool HasPhoto { get; set; }
    public string RollElectionName { get; set; } = string.Empty;
    public string RollVillageName { get; set; } = string.Empty;
    public string? RollPublicationDate { get; set; }
}

public class WardSummaryDto
{
    public int WardId { get; set; }
    public int WardNumber { get; set; }
    public string? WardName { get; set; }
    public int TotalVoters { get; set; }
    public int VerifiedVoters { get; set; }
    public int MinSerialNumber { get; set; }
    public int MaxSerialNumber { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ImportPreviewDto
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int TotalPages { get; set; }
    public int ExpectedVoters { get; set; }
    public int ExpectedWards { get; set; }
    public string District { get; set; } = string.Empty;
    public string Mandal { get; set; } = string.Empty;
    public string Village { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ImportValidationSummaryDto
{
    public int RollVersionId { get; set; }
    public int TotalExtracted { get; set; }
    public int ValidRecords { get; set; }
    public int WarningsCount { get; set; }
    public int ErrorsCount { get; set; }
    public bool CanApprove { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public class ExtractedVoterRecord
{
    public int GramPanchayatSerialNumber { get; set; }
    public int WardNumber { get; set; }
    public int? AssemblyPartNumber { get; set; }
    public int? AssemblySerialNumber { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string? RelativeName { get; set; }
    public string? Age { get; set; }
    public string? Gender { get; set; }
    public string? HouseNumber { get; set; }
    public string EPICNumber { get; set; } = string.Empty;
    public string NormalizedEPIC { get; set; } = string.Empty;
    public int SourcePageNumber { get; set; }
}
