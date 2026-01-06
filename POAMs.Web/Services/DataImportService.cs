using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public class DataImportService : IDataImportService
{
    private readonly ApplicationDbContext _context;

    public DataImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DataImportDashboardViewModel> GetDashboardDataAsync()
    {
        return new DataImportDashboardViewModel
        {
            NISTControlCount = await _context.NISTControls.CountAsync(),
            CCICount = await _context.CCIs.CountAsync(),
            DocumentationRequirementCount = await _context.ControlDocumentationRequirements.CountAsync()
        };
    }

    public async Task<ImportResultViewModel> ImportNISTCatalogAsync(Stream fileStream, bool replaceExisting = false)
    {
        var result = new ImportResultViewModel();

        try
        {
            using var reader = new StreamReader(fileStream);
            var json = await reader.ReadToEndAsync();

            var catalog = JsonSerializer.Deserialize<Dictionary<string, CatalogEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (catalog == null || catalog.Count == 0)
            {
                result.Success = false;
                result.Message = "Failed to parse catalog JSON or file is empty.";
                return result;
            }

            // If replacing, delete existing data
            if (replaceExisting)
            {
                // Must delete in correct order due to foreign keys
                var existingInstances = await _context.ControlDocumentationInstances.ToListAsync();
                _context.ControlDocumentationInstances.RemoveRange(existingInstances);

                var existingRequirements = await _context.ControlDocumentationRequirements.ToListAsync();
                _context.ControlDocumentationRequirements.RemoveRange(existingRequirements);

                var existingAssessments = await _context.NISTControlAssessments.ToListAsync();
                _context.NISTControlAssessments.RemoveRange(existingAssessments);

                var existingCcis = await _context.CCIs.ToListAsync();
                _context.CCIs.RemoveRange(existingCcis);

                var existingControls = await _context.NISTControls.ToListAsync();
                _context.NISTControls.RemoveRange(existingControls);

                await _context.SaveChangesAsync();
            }

            // Build lookup of existing control IDs
            var existingControlIdsList = await _context.NISTControls
                .Select(c => c.ControlId)
                .ToListAsync();
            var existingControlIds = existingControlIdsList.ToHashSet();

            foreach (var (controlId, entry) in catalog)
            {
                result.RecordsProcessed++;

                // Skip if already exists and not replacing
                if (existingControlIds.Contains(controlId))
                {
                    result.RecordsSkipped++;
                    continue;
                }

                try
                {
                    // Parse family from control ID (e.g., "AC" from "AC-2(1)")
                    var family = controlId.Split('-')[0];

                    // Check if withdrawn
                    var isWithdrawn = entry.ControlText?.Contains("[Withdrawn:") ?? false;

                    // Parse parent control ID for enhancements (e.g., "AC-2" from "AC-2(1)")
                    string? parentControlId = null;
                    if (controlId.Contains('('))
                    {
                        parentControlId = controlId.Substring(0, controlId.IndexOf('('));
                    }

                    var control = new NISTControl
                    {
                        ControlId = controlId,
                        Family = family,
                        Name = entry.Name ?? string.Empty,
                        ControlText = entry.ControlText ?? string.Empty,
                        Discussion = entry.Discussion,
                        RelatedControls = entry.RelatedControls != null
                            ? JsonSerializer.Serialize(entry.RelatedControls)
                            : null,
                        IsWithdrawn = isWithdrawn,
                        ParentControlId = parentControlId
                    };

                    _context.NISTControls.Add(control);
                    await _context.SaveChangesAsync();

                    // Add CCIs for this control
                    if (entry.CCIs != null)
                    {
                        foreach (var cciEntry in entry.CCIs)
                        {
                            var cci = new CCI
                            {
                                NISTControlId = control.Id,
                                CCINumber = cciEntry.CCI ?? string.Empty,
                                Definition = cciEntry.Definition ?? string.Empty
                            };
                            _context.CCIs.Add(cci);
                        }
                        await _context.SaveChangesAsync();
                    }

                    result.RecordsAdded++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error importing control {controlId}: {ex.Message}");
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? $"Successfully imported {result.RecordsAdded} controls."
                : $"Import completed with {result.Errors.Count} errors.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<ImportResultViewModel> ImportDocumentationMatrixAsync(Stream fileStream, bool replaceExisting = false)
    {
        var result = new ImportResultViewModel();

        try
        {
            using var reader = new StreamReader(fileStream);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                result.Success = false;
                result.Message = "CSV file is empty or missing data rows.";
                return result;
            }

            // Check if NIST controls exist
            var controlCount = await _context.NISTControls.CountAsync();
            if (controlCount == 0)
            {
                result.Success = false;
                result.Message = "NIST Controls must be imported before importing the Documentation Matrix.";
                return result;
            }

            // If replacing, delete existing documentation requirements and instances
            if (replaceExisting)
            {
                var existingInstances = await _context.ControlDocumentationInstances.ToListAsync();
                _context.ControlDocumentationInstances.RemoveRange(existingInstances);

                var existingRequirements = await _context.ControlDocumentationRequirements.ToListAsync();
                _context.ControlDocumentationRequirements.RemoveRange(existingRequirements);

                await _context.SaveChangesAsync();
            }

            // Build lookup of control IDs to database IDs
            var controlLookup = await _context.NISTControls
                .ToDictionaryAsync(c => c.ControlId, c => c.Id);

            // Track what we've already added (to handle duplicates in CSV)
            var existingKeys = await _context.ControlDocumentationRequirements
                .Select(d => new { d.NISTControlId, d.DocType })
                .ToListAsync();
            var processedKeys = new HashSet<(int, DocumentationType)>(
                existingKeys.Select(e => (e.NISTControlId, e.DocType)));

            // Skip header row
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.RecordsProcessed++;

                try
                {
                    // Parse CSV line (handle quoted fields with commas)
                    var fields = ParseCsvLine(line);
                    if (fields.Count < 5)
                    {
                        result.Warnings.Add($"Line {result.RecordsProcessed + 1}: Insufficient columns, skipped.");
                        result.RecordsSkipped++;
                        continue;
                    }

                    var controlId = fields[1].Trim(); // Control ID column
                    var docTypeStr = fields[3].Trim(); // Doc Type column
                    var priorityStr = fields[4].Trim(); // Priority column
                    var notes = fields.Count > 11 ? fields[11].Trim().Trim('"') : null; // Notes column

                    // Find the control
                    if (!controlLookup.TryGetValue(controlId, out var nistControlId))
                    {
                        result.Warnings.Add($"Control '{controlId}' not found in NIST catalog, skipped.");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Parse doc type
                    if (!Enum.TryParse<DocumentationType>(docTypeStr, true, out var docType))
                    {
                        result.Warnings.Add($"Invalid document type '{docTypeStr}' for control {controlId}, skipped.");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Check if we've already processed this control+doctype combination
                    var key = (nistControlId, docType);
                    if (processedKeys.Contains(key))
                    {
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Mark as processed
                    processedKeys.Add(key);

                    // Parse priority
                    var priority = priorityStr.ToLower() switch
                    {
                        "critical" => DocumentationPriority.Critical,
                        "high" => DocumentationPriority.High,
                        "medium" => DocumentationPriority.Medium,
                        "low" => DocumentationPriority.Low,
                        _ => DocumentationPriority.Medium
                    };

                    var requirement = new ControlDocumentationRequirement
                    {
                        NISTControlId = nistControlId,
                        DocType = docType,
                        Priority = priority,
                        Description = notes
                    };

                    _context.ControlDocumentationRequirements.Add(requirement);
                    result.RecordsAdded++;

                    // Batch save every 100 records
                    if (result.RecordsAdded % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Line {result.RecordsProcessed + 1}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? $"Successfully imported {result.RecordsAdded} documentation requirements."
                : $"Import completed with {result.Errors.Count} errors.";

            if (result.Warnings.Count > 0 && result.Success)
            {
                result.Message += $" ({result.Warnings.Count} warnings)";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    /// <summary>
    /// Parse a CSV line handling quoted fields with commas
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }

    // Helper classes for JSON deserialization
    private class CatalogEntry
    {
        public string? Name { get; set; }
        public string? ControlText { get; set; }
        public string? Discussion { get; set; }
        public List<string>? RelatedControls { get; set; }
        public List<CCIEntry>? CCIs { get; set; }
    }

    private class CCIEntry
    {
        public string? CCI { get; set; }
        public string? Definition { get; set; }
    }
}
