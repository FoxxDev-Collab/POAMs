using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public class NISTCatalogService : INISTCatalogService
{
    private readonly ApplicationDbContext _context;

    public NISTCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FamilySummaryViewModel>> GetFamilySummariesAsync(int? systemId = null)
    {
        var families = await _context.NISTControls
            .GroupBy(c => c.Family)
            .Select(g => new
            {
                Family = g.Key,
                TotalControls = g.Count(),
                TotalCCIs = g.Sum(c => c.CCIs.Count)
            })
            .ToListAsync();

        var summaries = new List<FamilySummaryViewModel>();

        foreach (var family in families)
        {
            var summary = new FamilySummaryViewModel
            {
                Family = family.Family,
                FamilyName = NISTFamilyNames.GetName(family.Family),
                TotalControls = family.TotalControls,
                TotalCCIs = family.TotalCCIs
            };

            if (systemId.HasValue)
            {
                // Get all assessments for this family/system, then find latest in memory
                var allAssessments = await _context.NISTControlAssessments
                    .Where(a => a.SystemId == systemId.Value && a.NISTControl.Family == family.Family)
                    .Select(a => new { a.NISTControlId, a.Status, a.AssessedDate, a.CreatedDate })
                    .ToListAsync();

                // Group in memory and get latest status for each control
                var latestStatuses = allAssessments
                    .GroupBy(a => a.NISTControlId)
                    .Select(g => g.OrderByDescending(a => a.AssessedDate ?? a.CreatedDate).First().Status)
                    .ToList();

                summary.Compliant = latestStatuses.Count(s => s == ControlAssessmentStatus.Compliant);
                summary.NonCompliant = latestStatuses.Count(s => s == ControlAssessmentStatus.NonCompliant);
                summary.NotApplicable = latestStatuses.Count(s => s == ControlAssessmentStatus.NotApplicable);
                summary.Inherited = latestStatuses.Count(s => s == ControlAssessmentStatus.Inherited);

                // Not assessed = total controls minus those with any assessment
                var controlsWithAssessments = latestStatuses.Count;
                summary.NotAssessed = summary.TotalControls - controlsWithAssessments;
            }
            else
            {
                summary.NotAssessed = summary.TotalControls;
            }

            summaries.Add(summary);
        }

        return summaries.OrderBy(s => s.Family).ToList();
    }

    public async Task<List<ControlListItemViewModel>> GetControlsByFamilyAsync(
        string family,
        int? systemId = null,
        string? search = null,
        ControlAssessmentStatus? status = null)
    {
        var query = _context.NISTControls
            .Where(c => c.Family == family.ToUpper())
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c =>
                c.ControlId.ToLower().Contains(search) ||
                c.Name.ToLower().Contains(search));
        }

        var controls = await query
            .Select(c => new ControlListItemViewModel
            {
                Id = c.Id,
                ControlId = c.ControlId,
                Name = c.Name,
                Family = c.Family,
                CCICount = c.CCIs.Count,
                IsWithdrawn = c.IsWithdrawn,
                IsEnhancement = c.ParentControlId != null,
                ParentControlId = c.ParentControlId
            })
            .ToListAsync();

        // Load assessment status if system is selected (latest assessment per control)
        if (systemId.HasValue)
        {
            var controlIds = controls.Select(c => c.Id).ToList();

            // Get all assessments, then find latest in memory (EF Core SQLite doesn't support complex GroupBy)
            var allAssessments = await _context.NISTControlAssessments
                .Where(a => a.SystemId == systemId.Value && controlIds.Contains(a.NISTControlId))
                .Select(a => new { a.NISTControlId, a.Status, a.RiskLevel, a.POAMId, a.AssessedDate, a.CreatedDate })
                .ToListAsync();

            // Group in memory and get latest for each control
            var latestAssessments = allAssessments
                .GroupBy(a => a.NISTControlId)
                .Select(g => g.OrderByDescending(a => a.AssessedDate ?? a.CreatedDate).First())
                .ToList();

            foreach (var control in controls)
            {
                var assessment = latestAssessments.FirstOrDefault(a => a.NISTControlId == control.Id);
                if (assessment != null)
                {
                    control.Status = assessment.Status;
                    control.RiskLevel = assessment.RiskLevel;
                    control.POAMId = assessment.POAMId;
                }
            }

            // Filter by status if specified
            if (status.HasValue)
            {
                if (status == ControlAssessmentStatus.NotAssessed)
                {
                    controls = controls.Where(c => c.Status == null).ToList();
                }
                else
                {
                    controls = controls.Where(c => c.Status == status).ToList();
                }
            }
        }

        // Sort naturally: AC-1, AC-2, AC-2(1), AC-2(2), AC-3, AC-10, AC-11...
        return controls
            .OrderBy(c => c.ControlId, new ControlIdComparer())
            .ToList();
    }

    /// <summary>
    /// Natural sort comparer for NIST control IDs (e.g., AC-1, AC-2, AC-2(1), AC-10)
    /// </summary>
    private class ControlIdComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var (famX, numX, enhX) = ParseControlId(x);
            var (famY, numY, enhY) = ParseControlId(y);

            // Compare family first (AC vs AU)
            var famCompare = string.Compare(famX, famY, StringComparison.OrdinalIgnoreCase);
            if (famCompare != 0) return famCompare;

            // Compare base number (1 vs 10)
            if (numX != numY) return numX.CompareTo(numY);

            // Compare enhancement number (null < 1 < 2)
            if (enhX == null && enhY == null) return 0;
            if (enhX == null) return -1;
            if (enhY == null) return 1;
            return enhX.Value.CompareTo(enhY.Value);
        }

        private static (string family, int number, int? enhancement) ParseControlId(string controlId)
        {
            // Examples: AC-1, AC-2(1), AC-10, SC-28(1)
            var family = "";
            var number = 0;
            int? enhancement = null;

            var dashIndex = controlId.IndexOf('-');
            if (dashIndex > 0)
            {
                family = controlId.Substring(0, dashIndex);
                var rest = controlId.Substring(dashIndex + 1);

                var parenIndex = rest.IndexOf('(');
                if (parenIndex > 0)
                {
                    // Has enhancement: "2(1)" -> number=2, enhancement=1
                    int.TryParse(rest.Substring(0, parenIndex), out number);
                    var enhStr = rest.Substring(parenIndex + 1).TrimEnd(')');
                    if (int.TryParse(enhStr, out var enh))
                        enhancement = enh;
                }
                else
                {
                    // No enhancement: "10" -> number=10
                    int.TryParse(rest, out number);
                }
            }

            return (family, number, enhancement);
        }
    }

    public async Task<NISTControl?> GetControlWithCCIsAsync(string controlId)
    {
        return await _context.NISTControls
            .Include(c => c.CCIs)
            .FirstOrDefaultAsync(c => c.ControlId == controlId);
    }

    public async Task<NISTControlAssessment?> GetLatestAssessmentAsync(string controlId, int systemId)
    {
        return await _context.NISTControlAssessments
            .Include(a => a.NISTControl)
            .Include(a => a.System)
            .Include(a => a.AssessedBy)
            .Include(a => a.POAM)
            .Where(a => a.NISTControl.ControlId == controlId && a.SystemId == systemId)
            .OrderByDescending(a => a.AssessedDate ?? a.CreatedDate)
            .FirstOrDefaultAsync();
    }

    public async Task<NISTControlAssessment?> GetAssessmentByIdAsync(int assessmentId)
    {
        return await _context.NISTControlAssessments
            .Include(a => a.NISTControl)
            .Include(a => a.System)
            .Include(a => a.AssessedBy)
            .Include(a => a.POAM)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);
    }

    public async Task<List<NISTControlAssessment>> GetAssessmentHistoryAsync(string controlId, int systemId)
    {
        return await _context.NISTControlAssessments
            .Include(a => a.NISTControl)
            .Include(a => a.System)
            .Include(a => a.AssessedBy)
            .Include(a => a.POAM)
            .Where(a => a.NISTControl.ControlId == controlId && a.SystemId == systemId)
            .OrderByDescending(a => a.AssessedDate ?? a.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<NISTControlAssessment>> GetAssessmentsForControlAsync(string controlId)
    {
        return await _context.NISTControlAssessments
            .Include(a => a.System)
            .Include(a => a.AssessedBy)
            .Include(a => a.POAM)
            .Where(a => a.NISTControl.ControlId == controlId)
            .ToListAsync();
    }

    public async Task<NISTControlAssessment> CreateAssessmentAsync(NISTAssessmentEditViewModel model, int assessorId)
    {
        var control = await _context.NISTControls.FirstOrDefaultAsync(c => c.ControlId == model.ControlId)
            ?? throw new ArgumentException($"Control {model.ControlId} not found");

        var assessment = new NISTControlAssessment
        {
            NISTControlId = control.Id,
            SystemId = model.SystemId,
            Status = model.Status,
            RiskLevel = model.Status == ControlAssessmentStatus.NonCompliant ? model.RiskLevel : null,
            Implementation = model.Implementation,
            Evidence = model.Evidence,
            Notes = model.Notes,
            AssessedById = assessorId,
            AssessedDate = DateTime.UtcNow,
            POAMId = model.POAMId
        };

        _context.NISTControlAssessments.Add(assessment);
        await _context.SaveChangesAsync();
        return assessment;
    }

    public async Task<NISTControlAssessment> UpdateAssessmentAsync(NISTAssessmentEditViewModel model, int assessorId)
    {
        if (!model.Id.HasValue)
            throw new ArgumentException("Assessment ID is required for update");

        var assessment = await _context.NISTControlAssessments.FindAsync(model.Id.Value)
            ?? throw new ArgumentException($"Assessment {model.Id} not found");

        assessment.Status = model.Status;
        assessment.RiskLevel = model.Status == ControlAssessmentStatus.NonCompliant ? model.RiskLevel : null;
        assessment.Implementation = model.Implementation;
        assessment.Evidence = model.Evidence;
        assessment.Notes = model.Notes;
        assessment.AssessedById = assessorId;
        assessment.AssessedDate = DateTime.UtcNow;
        assessment.POAMId = model.POAMId;

        await _context.SaveChangesAsync();
        return assessment;
    }

    public async Task<bool> DeleteAssessmentAsync(int assessmentId)
    {
        var assessment = await _context.NISTControlAssessments.FindAsync(assessmentId);
        if (assessment == null)
            return false;

        _context.NISTControlAssessments.Remove(assessment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<POAM> CreatePOAMFromControlAsync(string controlId, int systemId, int userId)
    {
        var control = await _context.NISTControls.FirstOrDefaultAsync(c => c.ControlId == controlId)
            ?? throw new ArgumentException($"Control {controlId} not found");

        // Generate POAM identifier
        var existingCount = await _context.POAMs.CountAsync(p => p.SystemId == systemId);
        var identifier = $"POAM-{systemId:D3}-{existingCount + 1:D4}";

        var poam = new POAM
        {
            SystemId = systemId,
            ItemIdentifier = identifier,
            WeaknessOrDeficiency = $"Non-compliant with {controlId}: {control.Name}",
            SecurityControl = controlId,
            RiskLevel = RiskLevel.Moderate, // Default, user can change
            Status = POAMStatus.Open,
            POCId = userId,
            ScheduledCompletionDate = DateTime.UtcNow.AddDays(90), // Default 90 days
            IdentifiedBy = "NIST Control Assessment"
        };

        _context.POAMs.Add(poam);
        await _context.SaveChangesAsync();

        // Link the latest assessment to this POAM
        var latestAssessment = await _context.NISTControlAssessments
            .Where(a => a.NISTControl.ControlId == controlId && a.SystemId == systemId)
            .OrderByDescending(a => a.AssessedDate ?? a.CreatedDate)
            .FirstOrDefaultAsync();

        if (latestAssessment != null)
        {
            latestAssessment.POAMId = poam.Id;
            await _context.SaveChangesAsync();
        }

        return poam;
    }

    public async Task<int> GetControlCountAsync()
    {
        return await _context.NISTControls.CountAsync();
    }

    public async Task<int> GetCCICountAsync()
    {
        return await _context.CCIs.CountAsync();
    }
}
