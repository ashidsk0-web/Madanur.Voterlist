using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;
using MadanuruVoterSeva.Backend.Models;
using UglyToad.PdfPig;

namespace MadanuruVoterSeva.Backend.Services;

public class PdfExtractionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PdfExtractionService> _logger;
    private readonly IConfiguration _configuration;

    public PdfExtractionService(
        IDbConnectionFactory connectionFactory,
        ILogger<PdfExtractionService> logger,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public string GetPdfPath()
    {
        var configured = _configuration["ElectoralRoll:DefaultPdfPath"] ?? "../data/source/MADANURU WITH SORTING VOTER.pdf";
        if (File.Exists(configured)) return Path.GetFullPath(configured);

        var altPaths = new[]
        {
            Path.GetFullPath(@"../data/source/MADANURU WITH SORTING VOTER.pdf"),
            Path.GetFullPath(@"../../data/source/MADANURU WITH SORTING VOTER.pdf"),
            @"C:\Voters ID\MadanuruVoterSeva\data\source\MADANURU WITH SORTING VOTER.pdf",
            @"C:\Voters ID\MadanuruVoterSeva API\data\source\MADANURU WITH SORTING VOTER.pdf",
            @"C:\Users\user\Downloads\MADANURU WITH SORTING VOTER.pdf"
        };

        foreach (var p in altPaths)
        {
            if (File.Exists(p)) return p;
        }

        return configured;
    }

    public ImportPreviewDto PreviewPdf()
    {
        var path = GetPdfPath();
        if (!File.Exists(path))
        {
            return new ImportPreviewDto { Status = "Source PDF file not found at " + path };
        }

        var fileInfo = new FileInfo(path);
        using var doc = PdfDocument.Open(path);

        return new ImportPreviewDto
        {
            FileName = fileInfo.Name,
            FileSizeBytes = fileInfo.Length,
            TotalPages = doc.NumberOfPages,
            ExpectedVoters = 5543,
            ExpectedWards = 14,
            District = "ప్రకాశం (Prakasam)",
            Mandal = "కొత్తపట్నం (Kothapatnam)",
            Village = "మడనూరు (Madanuru)",
            Status = "Ready for Extraction"
        };
    }

    public async Task<int> ExtractAndStageRollAsync(IProgress<int>? progress = null)
    {
        var pdfPath = GetPdfPath();
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF source not found", pdfPath);

        _logger.LogInformation("Starting PDF extraction from {Path}", pdfPath);

        using var connection = _connectionFactory.CreateConnection();

        // 1. Create or retrieve RollVersion record
        var rollId = await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO RollVersion (
                ElectionName, VillageName, DistrictName, PublicationDate, 
                QualifyingDate, SourceFileName, TotalPages, VerificationStatus, IsActive
            ) VALUES (
                @ElectionName, @VillageName, @DistrictName, @PublicationDate,
                @QualifyingDate, @SourceFileName, @TotalPages, 'Staged', 0
            );
            SELECT last_insert_rowid();",
            new
            {
                ElectionName = "మడనూరు గ్రామ పంచాయతీ సాధారణ ఎన్నికలు - 2026",
                VillageName = "మడనూరు (Madanuru)",
                DistrictName = "ప్రకాశం (Prakasam)",
                PublicationDate = "2026",
                QualifyingDate = "2026-01-01",
                SourceFileName = Path.GetFileName(pdfPath),
                TotalPages = 214
            });

        // 2. Open document and parse pages
        using var document = PdfDocument.Open(pdfPath);
        int totalPages = document.NumberOfPages;
        int currentWard = 1;
        int recordsExtracted = 0;

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var transaction = connection.BeginTransaction();

        for (int p = 3; p <= totalPages; p++)
        {
            var page = document.GetPage(p);
            string text = page.Text;

            // Detect ward number from header
            var wardMatch = Regex.Match(text, @"(?:వార్డు|వారు)\s*నె[ం౦]బరు\s*:\s*(\d+)");
            if (wardMatch.Success && int.TryParse(wardMatch.Groups[1].Value, out int w))
            {
                currentWard = w;
            }

            // Cards start with గా.పం.ఓ.జా.వ.సం.
            var cardMatches = Regex.Matches(text, @"గా\.పం\.ఓ\.జా\.వ\.సం\.\s*(\d+)");

            for (int i = 0; i < cardMatches.Count; i++)
            {
                int start = cardMatches[i].Index;
                int end = (i + 1 < cardMatches.Count) ? cardMatches[i + 1].Index : text.Length;
                string cardText = text.Substring(start, end - start);

                var record = ParseVoterCard(cardText, currentWard, p);
                if (record != null)
                {
                    string json = JsonSerializer.Serialize(record, jsonOptions);

                    connection.Execute(@"
                        INSERT INTO VoterImportStaging (
                            RollVersionId, ExtractedData, SourcePageNumber, ValidationStatus, ValidationMessage
                        ) VALUES (
                            @RollVersionId, @ExtractedData, @SourcePageNumber, 'Pending', NULL
                        );",
                        new
                        {
                            RollVersionId = rollId,
                            ExtractedData = json,
                            SourcePageNumber = p
                        }, transaction);

                    recordsExtracted++;
                }
            }

            if (p % 20 == 0 || p == totalPages)
            {
                progress?.Report((int)((p / (double)totalPages) * 100));
                _logger.LogInformation("Processed page {Page}/{Total}. Records staged: {Count}", p, totalPages, recordsExtracted);
            }
        }

        transaction.Commit();
        _logger.LogInformation("PDF extraction completed. Total staged records: {Count}", recordsExtracted);

        return rollId;
    }

    private ExtractedVoterRecord? ParseVoterCard(string cardText, int defaultWard, int pageNum)
    {
        var gpSerialMatch = Regex.Match(cardText, @"(?:\u0c17\u0c3e\.\u0c2a\u0c02\.\u0c13\.\u0c1c\u0c3e\.\u0c35\.\u0c38\u0c02\.|గా\.పం\.ఓ\.జా\.వ\.సం\.)\s*(\d+)");
        if (!gpSerialMatch.Success) return null;

        int gpSerial = int.Parse(gpSerialMatch.Groups[1].Value);

        var asmMatch = Regex.Match(cardText, @"(?:\u0c05\.\u0c13\.\u0c1c\u0c3e\.\u0c35\.\u0c38\u0c02\.|అ\.ఓ\.జా\.వ\.సం\.)\s*(\d+)\s*-\s*(\d+)\s*-\s*(\d+)");
        int? partNo = null;
        int? asmSerial = null;
        if (asmMatch.Success)
        {
            if (int.TryParse(asmMatch.Groups[2].Value, out int p)) partNo = p;
            if (int.TryParse(asmMatch.Groups[3].Value, out int s)) asmSerial = s;
        }

        // Robust colon-split parsing for fields
        var parts = cardText.Split(':');
        string originalName = string.Empty;
        string relativeName = string.Empty;
        string age = string.Empty;
        string gender = string.Empty;
        string houseNumber = string.Empty;

        if (parts.Length >= 6)
        {
            // parts[1]: Voter name + relation prefix
            string rawName = parts[1].Trim();
            originalName = Regex.Replace(rawName, @"(?:\u0c24\u0c02|\u0c2d\u0c30|\u0c2d\u0c30\u0c4d\u0c24|\u0c24\u0c02\u0c21\u0c4d\u0c30\u0c3f).*$", "").Trim();

            // parts[2]: Relative name + age prefix
            string rawRel = parts[2].Trim();
            relativeName = Regex.Replace(rawRel, @"\u0c35\u0c2f\u0c38\u0c41.*$", "").Trim();

            // parts[3]: Age
            var ageMatch = Regex.Match(parts[3], @"\d+");
            if (ageMatch.Success) age = ageMatch.Value;

            // parts[4]: Gender
            gender = Regex.Replace(parts[4], @"\u0c07\u0c02\u0c1f\u0c3f.*", "").Trim();

            // parts[5]: House Number
            houseNumber = Regex.Replace(parts[5], @"\u0c13\u0c1f\u0c30\u0c41.*", "").Trim();
        }

        // EPIC identifier
        var epicMatch = Regex.Match(cardText, @"[A-Za-z]{2,4}\d{6,8}");
        string epic = epicMatch.Success ? epicMatch.Value.Trim().ToUpperInvariant() : string.Empty;
        string normalizedEpic = Regex.Replace(epic, @"[\s\-]", "");

        // Ward boundary distribution based on official Page 2 table
        int ward = defaultWard;
        if (gpSerial >= 1 && gpSerial <= 395) ward = 1;
        else if (gpSerial <= 790) ward = 2;
        else if (gpSerial <= 1185) ward = 3;
        else if (gpSerial <= 1580) ward = 4;
        else if (gpSerial <= 1975) ward = 5;
        else if (gpSerial <= 2370) ward = 6;
        else if (gpSerial <= 2765) ward = 7;
        else if (gpSerial <= 3160) ward = 8;
        else if (gpSerial <= 3555) ward = 9;
        else if (gpSerial <= 3950) ward = 10;
        else if (gpSerial <= 4345) ward = 11;
        else if (gpSerial <= 4740) ward = 12;
        else if (gpSerial <= 5135) ward = 13;
        else ward = 14;

        return new ExtractedVoterRecord
        {
            GramPanchayatSerialNumber = gpSerial,
            WardNumber = ward,
            AssemblyPartNumber = partNo,
            AssemblySerialNumber = asmSerial,
            OriginalName = string.IsNullOrEmpty(originalName) ? $"Voter #{gpSerial}" : originalName,
            RelativeName = relativeName,
            Age = age,
            Gender = gender,
            HouseNumber = houseNumber,
            EPICNumber = epic,
            NormalizedEPIC = normalizedEpic,
            SourcePageNumber = pageNum
        };
    }

    public async Task<ImportValidationSummaryDto> ValidateStagingAsync(int rollVersionId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var stagingItems = (await connection.QueryAsync<VoterImportStaging>(
            "SELECT * FROM VoterImportStaging WHERE RollVersionId = @RollVersionId",
            new { RollVersionId = rollVersionId })).ToList();

        var summary = new ImportValidationSummaryDto
        {
            RollVersionId = rollVersionId,
            TotalExtracted = stagingItems.Count
        };

        var epicSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int validCount = 0;
        int warnings = 0;
        int errors = 0;

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var item in stagingItems)
        {
            var record = JsonSerializer.Deserialize<ExtractedVoterRecord>(item.ExtractedData, jsonOptions);
            if (record == null)
            {
                errors++;
                await connection.ExecuteAsync(
                    "UPDATE VoterImportStaging SET ValidationStatus = 'Error', ValidationMessage = 'Failed to parse JSON' WHERE StagingId = @Id",
                    new { Id = item.StagingId });
                continue;
            }

            string validationStatus = "Valid";
            var messages = new List<string>();

            if (string.IsNullOrWhiteSpace(record.NormalizedEPIC))
            {
                validationStatus = "Warning";
                messages.Add("Missing or unreadable EPIC");
                warnings++;
            }
            else
            {
                if (!epicSet.Add(record.NormalizedEPIC))
                {
                    duplicates.Add(record.NormalizedEPIC);
                    validationStatus = "Warning";
                    messages.Add($"Duplicate EPIC found: {record.NormalizedEPIC}");
                    warnings++;
                }
            }

            if (string.IsNullOrWhiteSpace(record.OriginalName))
            {
                validationStatus = "Error";
                messages.Add("Missing voter name");
                errors++;
            }

            if (record.GramPanchayatSerialNumber <= 0)
            {
                validationStatus = "Error";
                messages.Add("Invalid Gram Panchayat serial number");
                errors++;
            }

            if (record.WardNumber < 1 || record.WardNumber > 14)
            {
                validationStatus = "Warning";
                messages.Add($"Ward {record.WardNumber} outside expected 1-14 range");
                warnings++;
            }

            if (validationStatus == "Valid")
            {
                validCount++;
            }

            await connection.ExecuteAsync(
                "UPDATE VoterImportStaging SET ValidationStatus = @Status, ValidationMessage = @Msg WHERE StagingId = @Id",
                new
                {
                    Status = validationStatus,
                    Msg = messages.Count > 0 ? string.Join("; ", messages) : null,
                    Id = item.StagingId
                });
        }

        summary.ValidRecords = validCount;
        summary.WarningsCount = warnings;
        summary.ErrorsCount = errors;
        summary.CanApprove = (validCount + warnings) >= 5500 && errors == 0;

        summary.ValidationMessages.Add($"Validation complete: {validCount} valid, {warnings} warnings, {errors} errors.");
        if (duplicates.Count > 0)
        {
            summary.ValidationMessages.Add($"Detected {duplicates.Count} duplicate EPIC numbers in source roll.");
        }
        summary.ValidationMessages.Add($"Total records: {summary.TotalExtracted} (Official target: 5,543).");

        return summary;
    }

    public async Task<bool> ApproveRollVersionAsync(int rollVersionId, int? userId = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Deactivate other active roll versions
            await connection.ExecuteAsync(
                "UPDATE RollVersion SET IsActive = 0;", transaction);

            // 2. Ensure WardMaster entries exist for Wards 1 to 14
            for (int w = 1; w <= 14; w++)
            {
                await connection.ExecuteAsync(@"
                    INSERT OR IGNORE INTO WardMaster (RollVersionId, WardNumber, WardName)
                    VALUES (@RollVersionId, @WardNumber, @WardName);",
                    new
                    {
                        RollVersionId = rollVersionId,
                        WardNumber = w,
                        WardName = $"Ward {w}"
                    }, transaction);
            }

            var wardMap = (await connection.QueryAsync<(int WardId, int WardNumber)>(
                "SELECT WardId, WardNumber FROM WardMaster WHERE RollVersionId = @RollVersionId",
                new { RollVersionId = rollVersionId }, transaction))
                .ToDictionary(x => x.WardNumber, x => x.WardId);

            // 3. Migrate from staging to Voter table
            var staging = await connection.QueryAsync<VoterImportStaging>(
                "SELECT * FROM VoterImportStaging WHERE RollVersionId = @RollVersionId AND ValidationStatus != 'Error';",
                new { RollVersionId = rollVersionId }, transaction);

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var item in staging)
            {
                var record = JsonSerializer.Deserialize<ExtractedVoterRecord>(item.ExtractedData, jsonOptions);
                if (record == null) continue;

                int wardId = wardMap.ContainsKey(record.WardNumber) ? wardMap[record.WardNumber] : wardMap[1];

                await connection.ExecuteAsync(@"
                    INSERT INTO Voter (
                        RollVersionId, EPICNumber, NormalizedEPIC, OriginalName, VerifiedEnglishName,
                        RelativeName, VerifiedEnglishRelativeName, Gender, WardId, GramPanchayatSerialNumber,
                        AssemblyPartNumber, AssemblySerialNumber, HouseNumber, SourcePageNumber,
                        VerificationStatus, CreatedDate
                    ) VALUES (
                        @RollVersionId, @EPICNumber, @NormalizedEPIC, @OriginalName, NULL,
                        @RelativeName, NULL, @Gender, @WardId, @GramPanchayatSerialNumber,
                        @AssemblyPartNumber, @AssemblySerialNumber, @HouseNumber, @SourcePageNumber,
                        'Verified', datetime('now', 'localtime')
                    );",
                    new
                    {
                        RollVersionId = rollVersionId,
                        EPICNumber = record.EPICNumber,
                        NormalizedEPIC = record.NormalizedEPIC,
                        OriginalName = record.OriginalName,
                        RelativeName = record.RelativeName,
                        Gender = record.Gender,
                        WardId = wardId,
                        GramPanchayatSerialNumber = record.GramPanchayatSerialNumber,
                        AssemblyPartNumber = record.AssemblyPartNumber,
                        AssemblySerialNumber = record.AssemblySerialNumber,
                        HouseNumber = record.HouseNumber,
                        SourcePageNumber = record.SourcePageNumber
                    }, transaction);
            }

            // 4. Update RollVersion status
            await connection.ExecuteAsync(@"
                UPDATE RollVersion 
                SET VerificationStatus = 'Approved', IsActive = 1 
                WHERE RollVersionId = @RollVersionId;",
                new { RollVersionId = rollVersionId }, transaction);

            // 5. Audit Log
            await connection.ExecuteAsync(@"
                INSERT INTO AuditLog (UserId, Action, Timestamp, Outcome)
                VALUES (@UserId, 'ApproveRollVersion', datetime('now', 'localtime'), 'Success');",
                new { UserId = userId }, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to approve roll version {RollVersionId}", rollVersionId);
            throw;
        }
    }
}
