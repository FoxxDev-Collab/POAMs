using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using System.Text.RegularExpressions;

namespace POAMs.Web.Services;

public class ExcelImportService : IImportService
{
    private readonly ApplicationDbContext _context;

    public ExcelImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Dictionary<string, string>>> PreviewExcelAsync(Stream fileStream)
    {
        var results = new List<Dictionary<string, string>>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();

        // Find header row (look for "Item Identifier" in first 20 rows)
        int headerRow = FindHeaderRow(worksheet);
        if (headerRow == -1)
        {
            throw new Exception("Could not find header row. Looking for 'Item Identifier' column.");
        }

        // Get column mappings
        var columns = GetColumnMappings(worksheet, headerRow);

        // Read data rows (max 50 for preview)
        int dataRow = headerRow + 1;
        int count = 0;
        while (count < 50 && !worksheet.Cell(dataRow, 1).IsEmpty())
        {
            var row = new Dictionary<string, string>();
            foreach (var col in columns)
            {
                var cell = worksheet.Cell(dataRow, col.Value);
                row[col.Key] = cell.GetFormattedString();
            }
            results.Add(row);
            dataRow++;
            count++;
        }

        return Task.FromResult(results);
    }

    public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, int systemId, bool updateExisting = false)
    {
        var result = new ImportResult { Success = true };

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();

        // Find header row
        int headerRow = FindHeaderRow(worksheet);
        if (headerRow == -1)
        {
            result.Success = false;
            result.Errors.Add("Could not find header row. Looking for 'Item Identifier' column.");
            return result;
        }

        // Get column mappings
        var columns = GetColumnMappings(worksheet, headerRow);

        // Get existing POAMs for this system
        var existingPoams = await _context.POAMs
            .Where(p => p.SystemId == systemId)
            .ToDictionaryAsync(p => p.ItemIdentifier ?? "", p => p);

        // Get users for POC matching
        var users = await _context.Users.ToListAsync();

        // Read data rows
        int dataRow = headerRow + 1;
        while (!IsRowEmpty(worksheet, dataRow, columns.Values.Max()))
        {
            try
            {
                var itemId = GetCellValue(worksheet, dataRow, columns, "ItemIdentifier");

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    dataRow++;
                    continue;
                }

                // Check if POAM exists
                if (existingPoams.TryGetValue(itemId, out var existingPoam))
                {
                    if (updateExisting)
                    {
                        UpdatePoamFromRow(existingPoam, worksheet, dataRow, columns, users);
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                        result.Warnings.Add($"Row {dataRow}: POAM '{itemId}' already exists, skipped.");
                    }
                }
                else
                {
                    var newPoam = CreatePoamFromRow(worksheet, dataRow, columns, systemId, users);
                    _context.POAMs.Add(newPoam);
                    existingPoams[itemId] = newPoam;
                    result.ImportedCount++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {dataRow}: {ex.Message}");
            }

            dataRow++;
        }

        if (result.ImportedCount > 0 || result.UpdatedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    private int FindHeaderRow(IXLWorksheet worksheet)
    {
        for (int row = 1; row <= 20; row++)
        {
            for (int col = 1; col <= 15; col++)
            {
                var value = worksheet.Cell(row, col).GetString().Trim().ToLower();
                if (value.Contains("item identifier") || value == "item id")
                {
                    return row;
                }
            }
        }
        return -1;
    }

    private Dictionary<string, int> GetColumnMappings(IXLWorksheet worksheet, int headerRow)
    {
        var mappings = new Dictionary<string, int>();
        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "item identifier", "ItemIdentifier" },
            { "item id", "ItemIdentifier" },
            { "weakness or deficiency", "WeaknessOrDeficiency" },
            { "weakness", "WeaknessOrDeficiency" },
            { "deficiency", "WeaknessOrDeficiency" },
            { "security control", "SecurityControl" },
            { "control", "SecurityControl" },
            { "poc", "POC" },
            { "point of contact", "POC" },
            { "resources required", "ResourcesRequired" },
            { "resources", "ResourcesRequired" },
            { "scheduled completion date", "ScheduledCompletionDate" },
            { "completion date", "ScheduledCompletionDate" },
            { "due date", "ScheduledCompletionDate" },
            { "milestones with completion dates", "Milestones" },
            { "milestones", "Milestones" },
            { "changes to milestones", "MilestoneChanges" },
            { "changes", "MilestoneChanges" },
            { "weakness/ deficiency identified by", "IdentifiedBy" },
            { "identified by", "IdentifiedBy" },
            { "source", "IdentifiedBy" },
            { "risk level", "RiskLevel" },
            { "risk", "RiskLevel" },
            { "estimated cost", "EstimatedCost" },
            { "cost", "EstimatedCost" },
            { "status", "Status" },
            { "comments", "Comments" },
            { "notes", "Comments" }
        };

        for (int col = 1; col <= 20; col++)
        {
            var headerText = worksheet.Cell(headerRow, col).GetString().Trim().ToLower();
            // Remove newlines and extra spaces
            headerText = Regex.Replace(headerText, @"\s+", " ");

            foreach (var map in headerMap)
            {
                if (headerText.Contains(map.Key) && !mappings.ContainsKey(map.Value))
                {
                    mappings[map.Value] = col;
                    break;
                }
            }
        }

        return mappings;
    }

    private string GetCellValue(IXLWorksheet worksheet, int row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col))
            return "";

        return worksheet.Cell(row, col).GetString().Trim();
    }

    private DateTime? GetDateValue(IXLWorksheet worksheet, int row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col))
            return null;

        var cell = worksheet.Cell(row, col);

        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime();

        var stringVal = cell.GetString().Trim();
        if (DateTime.TryParse(stringVal, out var date))
            return date;

        return null;
    }

    private decimal? GetDecimalValue(IXLWorksheet worksheet, int row, Dictionary<string, int> columns, string columnName)
    {
        if (!columns.TryGetValue(columnName, out var col))
            return null;

        var cell = worksheet.Cell(row, col);

        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        var stringVal = cell.GetString().Trim().Replace("$", "").Replace(",", "");
        if (decimal.TryParse(stringVal, out var value))
            return value;

        return null;
    }

    private bool IsRowEmpty(IXLWorksheet worksheet, int row, int maxCol)
    {
        for (int col = 1; col <= Math.Min(maxCol, 5); col++)
        {
            if (!string.IsNullOrWhiteSpace(worksheet.Cell(row, col).GetString()))
                return false;
        }
        return true;
    }

    private POAM CreatePoamFromRow(IXLWorksheet worksheet, int row, Dictionary<string, int> columns, int systemId, List<User> users)
    {
        var poam = new POAM
        {
            SystemId = systemId,
            ItemIdentifier = GetCellValue(worksheet, row, columns, "ItemIdentifier"),
            WeaknessOrDeficiency = GetCellValue(worksheet, row, columns, "WeaknessOrDeficiency"),
            SecurityControl = GetCellValue(worksheet, row, columns, "SecurityControl"),
            ResourcesRequired = GetCellValue(worksheet, row, columns, "ResourcesRequired"),
            ScheduledCompletionDate = GetDateValue(worksheet, row, columns, "ScheduledCompletionDate") ?? DateTime.UtcNow.AddMonths(3),
            IdentifiedBy = GetCellValue(worksheet, row, columns, "IdentifiedBy"),
            EstimatedCost = GetDecimalValue(worksheet, row, columns, "EstimatedCost"),
            Comments = GetCellValue(worksheet, row, columns, "Comments"),
            OriginalPOAMDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow
        };

        // Parse risk level
        var riskText = GetCellValue(worksheet, row, columns, "RiskLevel").ToLower();
        poam.RiskLevel = riskText switch
        {
            var r when r.Contains("high") => RiskLevel.High,
            var r when r.Contains("med") || r.Contains("moderate") => RiskLevel.Moderate,
            _ => RiskLevel.Low
        };

        // Parse status
        var statusText = GetCellValue(worksheet, row, columns, "Status").ToLower();
        poam.Status = statusText switch
        {
            var s when s.Contains("draft") => POAMStatus.Draft,
            var s when s.Contains("open") => POAMStatus.Open,
            var s when s.Contains("ongoing") || s.Contains("in progress") => POAMStatus.Ongoing,
            var s when s.Contains("completed") || s.Contains("complete") => POAMStatus.Completed,
            var s when s.Contains("closed") => POAMStatus.Closed,
            _ => POAMStatus.Open
        };

        // Try to match POC
        var pocName = GetCellValue(worksheet, row, columns, "POC");
        if (!string.IsNullOrWhiteSpace(pocName))
        {
            var matchedUser = users.FirstOrDefault(u =>
                u.DisplayName.Contains(pocName, StringComparison.OrdinalIgnoreCase) ||
                pocName.Contains(u.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Equals(pocName, StringComparison.OrdinalIgnoreCase));

            if (matchedUser != null)
                poam.POCId = matchedUser.Id;
        }

        // Parse milestones
        var milestonesText = GetCellValue(worksheet, row, columns, "Milestones");
        if (!string.IsNullOrWhiteSpace(milestonesText))
        {
            poam.Milestones = ParseMilestones(milestonesText);
        }

        return poam;
    }

    private void UpdatePoamFromRow(POAM poam, IXLWorksheet worksheet, int row, Dictionary<string, int> columns, List<User> users)
    {
        var weakness = GetCellValue(worksheet, row, columns, "WeaknessOrDeficiency");
        if (!string.IsNullOrWhiteSpace(weakness))
            poam.WeaknessOrDeficiency = weakness;

        var secControl = GetCellValue(worksheet, row, columns, "SecurityControl");
        if (!string.IsNullOrWhiteSpace(secControl))
            poam.SecurityControl = secControl;

        var resources = GetCellValue(worksheet, row, columns, "ResourcesRequired");
        if (!string.IsNullOrWhiteSpace(resources))
            poam.ResourcesRequired = resources;

        var schedDate = GetDateValue(worksheet, row, columns, "ScheduledCompletionDate");
        if (schedDate.HasValue)
            poam.ScheduledCompletionDate = schedDate.Value;

        var identifiedBy = GetCellValue(worksheet, row, columns, "IdentifiedBy");
        if (!string.IsNullOrWhiteSpace(identifiedBy))
            poam.IdentifiedBy = identifiedBy;

        var cost = GetDecimalValue(worksheet, row, columns, "EstimatedCost");
        if (cost.HasValue)
            poam.EstimatedCost = cost;

        var comments = GetCellValue(worksheet, row, columns, "Comments");
        if (!string.IsNullOrWhiteSpace(comments))
            poam.Comments = comments;

        // Parse risk level
        var riskText = GetCellValue(worksheet, row, columns, "RiskLevel").ToLower();
        if (!string.IsNullOrWhiteSpace(riskText))
        {
            poam.RiskLevel = riskText switch
            {
                var r when r.Contains("high") => RiskLevel.High,
                var r when r.Contains("med") || r.Contains("moderate") => RiskLevel.Moderate,
                _ => RiskLevel.Low
            };
        }

        // Parse status
        var statusText = GetCellValue(worksheet, row, columns, "Status").ToLower();
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            poam.Status = statusText switch
            {
                var s when s.Contains("draft") => POAMStatus.Draft,
                var s when s.Contains("open") => POAMStatus.Open,
                var s when s.Contains("ongoing") || s.Contains("in progress") => POAMStatus.Ongoing,
                var s when s.Contains("completed") || s.Contains("complete") => POAMStatus.Completed,
                var s when s.Contains("closed") => POAMStatus.Closed,
                _ => poam.Status
            };
        }

        poam.LastUpdateDate = DateTime.UtcNow;
    }

    private List<Milestone> ParseMilestones(string milestonesText)
    {
        var milestones = new List<Milestone>();
        var lines = milestonesText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        int number = 1;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Try to parse formats like:
            // "1. Create STP = 01/15/2024"
            // "Create STP - 01/15/2024"
            // "Create STP (01/15/2024)"

            var milestone = new Milestone
            {
                MilestoneNumber = number,
                Status = MilestoneStatus.NotStarted
            };

            // Remove leading number if present
            var match = Regex.Match(trimmed, @"^\d+[\.\)]\s*(.+)");
            var content = match.Success ? match.Groups[1].Value : trimmed;

            // Try to extract date
            var dateMatch = Regex.Match(content, @"[=\-\(]\s*(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})[\)]?\s*$");
            if (dateMatch.Success)
            {
                var title = content.Substring(0, dateMatch.Index).Trim();
                milestone.Title = title.TrimEnd('=', '-', ' ');

                if (DateTime.TryParse(dateMatch.Groups[1].Value, out var dueDate))
                {
                    milestone.DueDate = dueDate;
                }
                else
                {
                    milestone.DueDate = DateTime.UtcNow.AddMonths(1);
                }
            }
            else
            {
                milestone.Title = content;
                milestone.DueDate = DateTime.UtcNow.AddMonths(1);
            }

            if (!string.IsNullOrWhiteSpace(milestone.Title))
            {
                milestones.Add(milestone);
                number++;
            }
        }

        return milestones;
    }
}
