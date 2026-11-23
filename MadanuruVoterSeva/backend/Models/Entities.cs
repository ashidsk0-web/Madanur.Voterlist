namespace MadanuruVoterSeva.Backend.Models;

public class RollVersion
{
    public int RollVersionId { get; set; }
    public string ElectionName { get; set; } = string.Empty;
    public string VillageName { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string? PublicationDate { get; set; }
    public string? QualifyingDate { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
    public bool IsActive { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
}

public class WardMaster
{
    public int WardId { get; set; }
    public int RollVersionId { get; set; }
    public int WardNumber { get; set; }
    public string? WardName { get; set; }
}

public class Voter
{
    public int VoterId { get; set; }
    public int RollVersionId { get; set; }
    public string EPICNumber { get; set; } = string.Empty;
    public string NormalizedEPIC { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? VerifiedEnglishName { get; set; }
    public string? RelativeName { get; set; }
    public string? VerifiedEnglishRelativeName { get; set; }
    public string? Gender { get; set; }
    public int WardId { get; set; }
    public int GramPanchayatSerialNumber { get; set; }
    public int? AssemblyPartNumber { get; set; }
    public int? AssemblySerialNumber { get; set; }
    public string? HouseNumber { get; set; }
    public int SourcePageNumber { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
    public string CreatedDate { get; set; } = string.Empty;
}

public class PollingStation
{
    public int PollingStationId { get; set; }
    public int RollVersionId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public string? StationAddress { get; set; }
    public string? VerificationSource { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
}

public class VoterPollingStation
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public int PollingStationId { get; set; }
    public string? AssignmentSource { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
}

public class VoterImportStaging
{
    public int StagingId { get; set; }
    public int RollVersionId { get; set; }
    public string ExtractedData { get; set; } = string.Empty;
    public int SourcePageNumber { get; set; }
    public string ValidationStatus { get; set; } = "Pending";
    public string? ValidationMessage { get; set; }
}

public class AppUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public bool IsActive { get; set; } = true;
    public string CreatedDate { get; set; } = string.Empty;
}

public class AuditLog
{
    public int AuditId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
}
