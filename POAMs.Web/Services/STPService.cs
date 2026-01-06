using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public class STPService : ISTPService
{
    private readonly ApplicationDbContext _context;

    public STPService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region SecurityTestPlan CRUD

    public async Task<List<SecurityTestPlan>> GetAllAsync(STPType? type = null)
    {
        var query = _context.SecurityTestPlans
            .Include(s => s.System)
            .Include(s => s.POAM)
            .Include(s => s.LeadAssessor)
            .Include(s => s.TestCases)
            .Include(s => s.ControlTestCases)
            .Include(s => s.NessusTestCases)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);

        return await query.OrderByDescending(s => s.CreatedDate).ToListAsync();
    }

    public async Task<SecurityTestPlan?> GetByIdAsync(int id)
    {
        return await _context.SecurityTestPlans
            .Include(s => s.System)
            .Include(s => s.POAM)
            .Include(s => s.LeadAssessor)
            .Include(s => s.ISSM)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SecurityTestPlan?> GetByIdWithTestCasesAsync(int id)
    {
        return await _context.SecurityTestPlans
            .Include(s => s.System)
            .Include(s => s.POAM)
            .Include(s => s.LeadAssessor)
            .Include(s => s.ISSM)
            .Include(s => s.CreatedBy)
            .Include(s => s.TestCases.OrderBy(t => t.SortOrder).ThenBy(t => t.VulnId))
                .ThenInclude(t => t.Tester)
            .Include(s => s.ControlTestCases.OrderBy(c => c.SortOrder).ThenBy(c => c.ControlId))
            .Include(s => s.NessusTestCases.OrderBy(n => n.SortOrder).ThenBy(n => n.PluginId))
                .ThenInclude(n => n.Tester)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<SecurityTestPlan>> GetBySystemAsync(int systemId, STPType? type = null)
    {
        var query = _context.SecurityTestPlans
            .Include(s => s.TestCases)
            .Include(s => s.ControlTestCases)
            .Include(s => s.NessusTestCases)
            .Where(s => s.SystemId == systemId);

        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);

        return await query.OrderByDescending(s => s.CreatedDate).ToListAsync();
    }

    public async Task<List<SecurityTestPlan>> GetByPOAMAsync(int poamId)
    {
        return await _context.SecurityTestPlans
            .Include(s => s.TestCases)
            .Include(s => s.ControlTestCases)
            .Include(s => s.NessusTestCases)
            .Where(s => s.POAMId == poamId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }

    public async Task<SecurityTestPlan> CreateAsync(SecurityTestPlan stp)
    {
        if (string.IsNullOrEmpty(stp.STPIdentifier))
        {
            stp.STPIdentifier = await GenerateIdentifierAsync(stp.SystemId, stp.Type);
        }

        _context.SecurityTestPlans.Add(stp);
        await _context.SaveChangesAsync();
        return stp;
    }

    public async Task<SecurityTestPlan> UpdateAsync(SecurityTestPlan stp)
    {
        _context.SecurityTestPlans.Update(stp);
        await _context.SaveChangesAsync();
        return stp;
    }

    public async Task DeleteAsync(int id)
    {
        var stp = await _context.SecurityTestPlans.FindAsync(id);
        if (stp != null)
        {
            _context.SecurityTestPlans.Remove(stp);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region STIG Test Cases

    public async Task<STPTestCase?> GetSTIGTestCaseByIdAsync(int id)
    {
        return await _context.STPTestCases
            .Include(t => t.SecurityTestPlan)
            .Include(t => t.Tester)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<STPTestCase> AddSTIGTestCaseAsync(STPTestCase testCase)
    {
        var maxOrder = await _context.STPTestCases
            .Where(t => t.SecurityTestPlanId == testCase.SecurityTestPlanId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;
        testCase.SortOrder = maxOrder + 1;

        _context.STPTestCases.Add(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task<STPTestCase> UpdateSTIGTestCaseAsync(STPTestCase testCase)
    {
        _context.STPTestCases.Update(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task DeleteSTIGTestCaseAsync(int id)
    {
        var testCase = await _context.STPTestCases.FindAsync(id);
        if (testCase != null)
        {
            _context.STPTestCases.Remove(testCase);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<STPTestCase>> AddSTIGTestCasesAsync(int stpId, List<STPTestCase> testCases)
    {
        var maxOrder = await _context.STPTestCases
            .Where(t => t.SecurityTestPlanId == stpId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        foreach (var testCase in testCases)
        {
            testCase.SecurityTestPlanId = stpId;
            testCase.SortOrder = ++maxOrder;
            _context.STPTestCases.Add(testCase);
        }

        await _context.SaveChangesAsync();
        return testCases;
    }

    public async Task UpdateSTIGTestCaseStatusAsync(int testCaseId, TestCaseStatus status, string actualResult, string evidence, int? testerId)
    {
        var testCase = await _context.STPTestCases.FindAsync(testCaseId);
        if (testCase != null)
        {
            testCase.Status = status;
            testCase.ActualResult = actualResult;
            testCase.Evidence = evidence;
            testCase.TesterId = testerId;
            testCase.TestDate = DateTime.UtcNow;
            testCase.RetestRequired = status == TestCaseStatus.Fail;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Security Control Test Cases

    public async Task<SecurityControlTestCase?> GetControlTestCaseByIdAsync(int id)
    {
        return await _context.SecurityControlTestCases
            .Include(t => t.SecurityTestPlan)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<SecurityControlTestCase> AddControlTestCaseAsync(SecurityControlTestCase testCase)
    {
        var maxOrder = await _context.SecurityControlTestCases
            .Where(t => t.SecurityTestPlanId == testCase.SecurityTestPlanId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;
        testCase.SortOrder = maxOrder + 1;

        _context.SecurityControlTestCases.Add(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task<SecurityControlTestCase> UpdateControlTestCaseAsync(SecurityControlTestCase testCase)
    {
        _context.SecurityControlTestCases.Update(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task DeleteControlTestCaseAsync(int id)
    {
        var testCase = await _context.SecurityControlTestCases.FindAsync(id);
        if (testCase != null)
        {
            _context.SecurityControlTestCases.Remove(testCase);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<SecurityControlTestCase>> AddControlTestCasesAsync(int stpId, List<SecurityControlTestCase> testCases)
    {
        var maxOrder = await _context.SecurityControlTestCases
            .Where(t => t.SecurityTestPlanId == stpId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        foreach (var testCase in testCases)
        {
            testCase.SecurityTestPlanId = stpId;
            testCase.SortOrder = ++maxOrder;
            _context.SecurityControlTestCases.Add(testCase);
        }

        await _context.SaveChangesAsync();
        return testCases;
    }

    public async Task UpdateControlTestCaseStatusAsync(int testCaseId, ControlTestStatus status, string comments)
    {
        var testCase = await _context.SecurityControlTestCases.FindAsync(testCaseId);
        if (testCase != null)
        {
            testCase.Status = status;
            testCase.Comments = comments;
            testCase.ReviewDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Nessus Test Cases

    public async Task<NessusTestCase?> GetNessusTestCaseByIdAsync(int id)
    {
        return await _context.NessusTestCases
            .Include(t => t.SecurityTestPlan)
            .Include(t => t.Tester)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<NessusTestCase> AddNessusTestCaseAsync(NessusTestCase testCase)
    {
        var maxOrder = await _context.NessusTestCases
            .Where(t => t.SecurityTestPlanId == testCase.SecurityTestPlanId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;
        testCase.SortOrder = maxOrder + 1;

        _context.NessusTestCases.Add(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task<NessusTestCase> UpdateNessusTestCaseAsync(NessusTestCase testCase)
    {
        _context.NessusTestCases.Update(testCase);
        await _context.SaveChangesAsync();
        return testCase;
    }

    public async Task DeleteNessusTestCaseAsync(int id)
    {
        var testCase = await _context.NessusTestCases.FindAsync(id);
        if (testCase != null)
        {
            _context.NessusTestCases.Remove(testCase);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<NessusTestCase>> AddNessusTestCasesAsync(int stpId, List<NessusTestCase> testCases)
    {
        var maxOrder = await _context.NessusTestCases
            .Where(t => t.SecurityTestPlanId == stpId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        foreach (var testCase in testCases)
        {
            testCase.SecurityTestPlanId = stpId;
            testCase.SortOrder = ++maxOrder;
            _context.NessusTestCases.Add(testCase);
        }

        await _context.SaveChangesAsync();
        return testCases;
    }

    public async Task UpdateNessusTestCaseStatusAsync(int testCaseId, VulnTestStatus status, string actualResult, string evidence, int? testerId)
    {
        var testCase = await _context.NessusTestCases.FindAsync(testCaseId);
        if (testCase != null)
        {
            testCase.Status = status;
            testCase.ActualResult = actualResult;
            testCase.Evidence = evidence;
            testCase.TesterId = testerId;
            testCase.TestDate = DateTime.UtcNow;
            testCase.RescanRequired = status == VulnTestStatus.NotRemediated || status == VulnTestStatus.PartiallyRemediated;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Utility Methods

    public async Task<string> GenerateIdentifierAsync(int systemId, STPType type)
    {
        var year = DateTime.UtcNow.Year;
        var typePrefix = type switch
        {
            STPType.STIG => "STIG",
            STPType.Nessus => "VULN",
            STPType.SecurityControl => "CTRL",
            _ => "STP"
        };
        var prefix = $"{typePrefix}-{year}-";

        var lastStp = await _context.SecurityTestPlans
            .Where(s => s.STPIdentifier.StartsWith(prefix))
            .OrderByDescending(s => s.STPIdentifier)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastStp != null)
        {
            var lastNum = lastStp.STPIdentifier.Substring(prefix.Length);
            if (int.TryParse(lastNum, out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"{prefix}{nextNumber:D3}";
    }

    #endregion

    #region Export

    public byte[] ExportToExcel(SecurityTestPlan stp)
    {
        return stp.Type switch
        {
            STPType.STIG => ExportSTIGToExcel(stp),
            STPType.Nessus => ExportNessusToExcel(stp),
            STPType.SecurityControl => ExportSecurityControlToExcel(stp),
            _ => ExportSTIGToExcel(stp)
        };
    }

    private byte[] ExportSTIGToExcel(SecurityTestPlan stp)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Test Plan Header
        var headerSheet = workbook.Worksheets.Add("Test Plan");
        CreateSTIGHeaderSheet(headerSheet, stp);

        // Sheet 2: Test Procedures
        var testSheet = workbook.Worksheets.Add("Test Procedures");
        CreateSTIGTestProceduresSheet(testSheet, stp);

        // Sheet 3: Summary
        var summarySheet = workbook.Worksheets.Add("Summary");
        CreateSTIGSummarySheet(summarySheet, stp);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] ExportNessusToExcel(SecurityTestPlan stp)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Test Plan Header
        var headerSheet = workbook.Worksheets.Add("Test Plan");
        CreateNessusHeaderSheet(headerSheet, stp);

        // Sheet 2: Vulnerabilities
        var vulnSheet = workbook.Worksheets.Add("Vulnerabilities");
        CreateNessusVulnerabilitiesSheet(vulnSheet, stp);

        // Sheet 3: Summary
        var summarySheet = workbook.Worksheets.Add("Summary");
        CreateNessusSummarySheet(summarySheet, stp);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] ExportSecurityControlToExcel(SecurityTestPlan stp)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Cover Page
        var coverSheet = workbook.Worksheets.Add("Cover Page");
        CreateSecurityControlCoverSheet(coverSheet, stp);

        // Sheet 2: Information Page
        var infoSheet = workbook.Worksheets.Add("Information");
        CreateSecurityControlInfoSheet(infoSheet, stp);

        // Sheet 3+: Control Family sheets
        var controlFamilies = stp.ControlTestCases
            .GroupBy(c => c.ControlFamily)
            .OrderBy(g => g.Key);

        foreach (var family in controlFamilies)
        {
            var familySheet = workbook.Worksheets.Add(family.Key);
            CreateSecurityControlFamilySheet(familySheet, stp, family.ToList());
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #region STIG Export Helpers

    private void CreateSTIGHeaderSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        ws.Cell("A1").Value = "SECURITY TEST PLAN (STP)";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Range("A1:D1").Merge();

        ws.Cell("A3").Value = "Test Plan Identification";
        ws.Cell("A3").Style.Font.Bold = true;
        ws.Range("A3:D3").Merge();
        ws.Range("A3:D3").Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 5;
        AddHeaderRow(ws, row++, "System Name:", stp.System?.SystemName ?? "", "Host/Asset Name:", stp.HostName);
        AddHeaderRow(ws, row++, "STIG Name:", stp.STIGName, "IP Address:", stp.IPAddress);
        AddHeaderRow(ws, row++, "STIG Version/Release:", stp.STIGVersion, "OS/Platform:", stp.OSPlatform);
        AddHeaderRow(ws, row++, "STIG Release Date:", stp.STIGReleaseDate?.ToString("yyyy-MM-dd") ?? "", "Environment:", stp.Environment);
        AddHeaderRow(ws, row++, "Test Plan Version:", stp.TestPlanVersion, "Classification:", stp.Classification);
        AddHeaderRow(ws, row++, "Test Plan Date:", stp.TestPlanDate.ToString("yyyy-MM-dd"), "MAC/CAL:", stp.MACCAL);

        row += 2;
        ws.Cell(row, 1).Value = "Testing Information";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Merge();
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
        row += 2;

        AddHeaderRow(ws, row++, "Lead Assessor:", stp.LeadAssessor?.DisplayName ?? "", "Authorizing Official:", stp.AuthorizingOfficial);
        AddHeaderRow(ws, row++, "Assessment Team:", stp.AssessmentTeam, "ISSM:", stp.ISSM?.DisplayName ?? "");
        AddHeaderRow(ws, row++, "Test Start Date:", stp.TestStartDate?.ToString("yyyy-MM-dd") ?? "", "System Owner:", stp.SystemOwner);
        AddHeaderRow(ws, row++, "Test End Date:", stp.TestEndDate?.ToString("yyyy-MM-dd") ?? "", "", "");

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 35;
    }

    private void CreateSTIGTestProceduresSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        var headers = new[] { "Vuln ID", "Rule ID", "Severity", "Rule Version", "Title", "Expected Result", "Actual Result", "Evidence/Notes", "Status", "Tester", "Test Date", "Retest Required" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row = 2;
        foreach (var testCase in stp.TestCases.OrderBy(t => t.SortOrder).ThenBy(t => t.VulnId))
        {
            ws.Cell(row, 1).Value = testCase.VulnId;
            ws.Cell(row, 2).Value = testCase.RuleId;
            ws.Cell(row, 3).Value = testCase.Severity.ToString().Replace("_", " ");
            ws.Cell(row, 4).Value = testCase.RuleVersion;
            ws.Cell(row, 5).Value = testCase.Title;
            ws.Cell(row, 6).Value = testCase.ExpectedResult;
            ws.Cell(row, 7).Value = testCase.ActualResult;
            ws.Cell(row, 8).Value = testCase.Evidence;
            ws.Cell(row, 9).Value = testCase.Status.ToString();
            ws.Cell(row, 10).Value = testCase.Tester?.DisplayName ?? "";
            ws.Cell(row, 11).Value = testCase.TestDate?.ToString("MM/dd/yyyy") ?? "";
            ws.Cell(row, 12).Value = testCase.RetestRequired ? "Yes" : "No";

            ws.Cell(row, 3).Style.Fill.BackgroundColor = testCase.Severity switch
            {
                STIGSeverity.CAT_I => XLColor.LightPink,
                STIGSeverity.CAT_II => XLColor.LightYellow,
                _ => XLColor.LightGreen
            };

            ws.Cell(row, 9).Style.Fill.BackgroundColor = testCase.Status switch
            {
                TestCaseStatus.Pass => XLColor.LightGreen,
                TestCaseStatus.Fail => XLColor.LightPink,
                TestCaseStatus.NotApplicable => XLColor.LightGray,
                _ => XLColor.White
            };

            for (int col = 1; col <= 12; col++)
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(5).Width = 50;
        ws.Column(8).Width = 40;
    }

    private void CreateSTIGSummarySheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        ws.Cell("A1").Value = "TEST EXECUTION SUMMARY";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A3").Value = "Test Statistics";
        ws.Cell("A3").Style.Font.Bold = true;
        ws.Cell("C3").Value = "By Severity";
        ws.Cell("C3").Style.Font.Bold = true;

        int row = 4;
        ws.Cell(row, 1).Value = "Total Test Cases:";
        ws.Cell(row, 2).Value = stp.TotalTestCases;
        ws.Cell(row, 3).Value = "CAT I Total:";
        ws.Cell(row, 4).Value = stp.CatITotal;
        row++;

        ws.Cell(row, 1).Value = "Pass:";
        ws.Cell(row, 2).Value = stp.PassCount;
        ws.Cell(row, 3).Value = "CAT I Fail:";
        ws.Cell(row, 4).Value = stp.CatIFail;
        if (stp.CatIFail > 0) ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightPink;
        row++;

        ws.Cell(row, 1).Value = "Fail:";
        ws.Cell(row, 2).Value = stp.FailCount;
        if (stp.FailCount > 0) ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightPink;
        ws.Cell(row, 3).Value = "CAT II Total:";
        ws.Cell(row, 4).Value = stp.CatIITotal;
        row++;

        ws.Cell(row, 1).Value = "Not Applicable:";
        ws.Cell(row, 2).Value = stp.NotApplicableCount;
        ws.Cell(row, 3).Value = "CAT II Fail:";
        ws.Cell(row, 4).Value = stp.CatIIFail;
        row++;

        ws.Cell(row, 1).Value = "Not Tested:";
        ws.Cell(row, 2).Value = stp.NotTestedCount;
        ws.Cell(row, 3).Value = "CAT III Total:";
        ws.Cell(row, 4).Value = stp.CatIIITotal;
        row++;

        ws.Cell(row, 1).Value = "Compliance Rate:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = $"{stp.ComplianceRate}%";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = "CAT III Fail:";
        ws.Cell(row, 4).Value = stp.CatIIIFail;

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 15;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 15;
    }

    #endregion

    #region Nessus Export Helpers

    private void CreateNessusHeaderSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        ws.Cell("A1").Value = "VULNERABILITY TEST PLAN";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Range("A1:D1").Merge();

        int row = 3;
        AddHeaderRow(ws, row++, "System Name:", stp.System?.SystemName ?? "", "Host/Asset Name:", stp.HostName);
        AddHeaderRow(ws, row++, "Scan Policy:", stp.ScanPolicyName, "IP Address:", stp.IPAddress);
        AddHeaderRow(ws, row++, "Scanner Version:", stp.ScannerVersion, "OS/Platform:", stp.OSPlatform);
        AddHeaderRow(ws, row++, "Scan Date:", stp.ScanDate?.ToString("yyyy-MM-dd") ?? "", "Environment:", stp.Environment);
        AddHeaderRow(ws, row++, "Test Plan Date:", stp.TestPlanDate.ToString("yyyy-MM-dd"), "Classification:", stp.Classification);

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 35;
    }

    private void CreateNessusVulnerabilitiesSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        var headers = new[] { "Plugin ID", "Plugin Name", "CVE", "CVSS", "Severity", "Synopsis", "Affected Hosts", "Remediation Steps", "Status", "Evidence", "Tester", "Test Date" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row = 2;
        foreach (var vuln in stp.NessusTestCases.OrderBy(n => n.SortOrder))
        {
            ws.Cell(row, 1).Value = vuln.PluginId;
            ws.Cell(row, 2).Value = vuln.PluginName;
            ws.Cell(row, 3).Value = vuln.CVE;
            ws.Cell(row, 4).Value = vuln.CVSSScore;
            ws.Cell(row, 5).Value = vuln.Severity.ToString();
            ws.Cell(row, 6).Value = vuln.Synopsis;
            ws.Cell(row, 7).Value = vuln.AffectedHosts;
            ws.Cell(row, 8).Value = vuln.RemediationSteps;
            ws.Cell(row, 9).Value = vuln.Status.ToString();
            ws.Cell(row, 10).Value = vuln.Evidence;
            ws.Cell(row, 11).Value = vuln.Tester?.DisplayName ?? "";
            ws.Cell(row, 12).Value = vuln.TestDate?.ToString("MM/dd/yyyy") ?? "";

            ws.Cell(row, 5).Style.Fill.BackgroundColor = vuln.Severity switch
            {
                NessusSeverity.Critical => XLColor.Red,
                NessusSeverity.High => XLColor.LightPink,
                NessusSeverity.Medium => XLColor.LightYellow,
                NessusSeverity.Low => XLColor.LightGreen,
                _ => XLColor.LightGray
            };

            ws.Cell(row, 9).Style.Fill.BackgroundColor = vuln.Status switch
            {
                VulnTestStatus.Remediated => XLColor.LightGreen,
                VulnTestStatus.NotRemediated => XLColor.LightPink,
                VulnTestStatus.AcceptedRisk => XLColor.LightYellow,
                _ => XLColor.White
            };

            for (int col = 1; col <= 12; col++)
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateNessusSummarySheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        ws.Cell("A1").Value = "VULNERABILITY SUMMARY";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        int row = 3;
        ws.Cell(row, 1).Value = "Total Vulnerabilities:";
        ws.Cell(row, 2).Value = stp.TotalVulnerabilities;
        row++;

        ws.Cell(row, 1).Value = "Critical:";
        ws.Cell(row, 2).Value = stp.CriticalCount;
        if (stp.CriticalCount > 0) ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.Red;
        row++;

        ws.Cell(row, 1).Value = "High:";
        ws.Cell(row, 2).Value = stp.HighCount;
        if (stp.HighCount > 0) ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightPink;
        row++;

        ws.Cell(row, 1).Value = "Medium:";
        ws.Cell(row, 2).Value = stp.MediumCount;
        row++;

        ws.Cell(row, 1).Value = "Low:";
        ws.Cell(row, 2).Value = stp.LowCount;
        row++;

        ws.Cell(row, 1).Value = "Remediated:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = stp.VulnsRemediated;
        ws.Cell(row, 2).Style.Font.Bold = true;
    }

    #endregion

    #region Security Control Export Helpers

    private void CreateSecurityControlCoverSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        ws.Cell("B3").Value = "Post System Implementation";
        ws.Cell("B3").Style.Font.Bold = true;
        ws.Cell("B3").Style.Font.FontSize = 16;

        ws.Cell("B4").Value = "Critical Controls";
        ws.Cell("B4").Style.Font.Bold = true;
        ws.Cell("B4").Style.Font.FontSize = 14;

        ws.Cell("B5").Value = "On-Site Test Plan";
        ws.Cell("B5").Style.Font.FontSize = 14;

        ws.Cell("B7").Value = stp.OSPlatform;
        ws.Cell("B7").Style.Font.Bold = true;

        ws.Cell("B9").Value = stp.TestPlanDate.ToString("dd MMMM yyyy").ToUpper();

        ws.Column(2).Width = 50;
    }

    private void CreateSecurityControlInfoSheet(IXLWorksheet ws, SecurityTestPlan stp)
    {
        int row = 1;

        ws.Cell(row, 1).Value = "Assessment Date(s):";
        ws.Cell(row, 2).Value = stp.TestStartDate?.ToString("MM/dd/yyyy") ?? "";
        row++;

        ws.Cell(row, 1).Value = "Host Assessed:";
        ws.Cell(row, 2).Value = stp.HostName;
        row++;

        ws.Cell(row, 1).Value = "Assessor 1 / Position:";
        ws.Cell(row, 2).Value = stp.LeadAssessor?.DisplayName ?? "";
        row++;

        ws.Cell(row, 1).Value = "Assessor 2 / Position:";
        ws.Cell(row, 2).Value = $"{stp.Assessor2Name} / {stp.Assessor2Position}";
        row++;

        ws.Cell(row, 1).Value = "Privileged User:";
        ws.Cell(row, 2).Value = stp.PrivilegedUser;
        row++;

        ws.Cell(row, 1).Value = "Authorized User:";
        ws.Cell(row, 2).Value = stp.AuthorizedUser;
        row++;

        row++;
        ws.Cell(row, 1).Value = "% Controls Assessed:";
        ws.Cell(row, 2).Value = $"{stp.ControlsAssessedPercent}%";
        row++;

        ws.Cell(row, 1).Value = "% Controls Compliant:";
        ws.Cell(row, 2).Value = $"{stp.ControlsCompliantPercent}%";
        row++;

        ws.Cell(row, 1).Value = "% Controls Non-Compliant:";
        ws.Cell(row, 2).Value = $"{100 - stp.ControlsCompliantPercent}%";

        ws.Column(1).Width = 25;
        ws.Column(2).Width = 40;
    }

    private void CreateSecurityControlFamilySheet(IXLWorksheet ws, SecurityTestPlan stp, List<SecurityControlTestCase> controls)
    {
        int row = 1;

        foreach (var control in controls.OrderBy(c => c.ControlId))
        {
            // Control header
            ws.Cell(row, 1).Value = control.ControlId;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;

            ws.Cell(row, 2).Value = control.ControlTitle;
            ws.Cell(row, 2).Style.Font.Bold = true;

            // Status color
            ws.Cell(row, 3).Value = control.Status.ToString();
            ws.Cell(row, 3).Style.Fill.BackgroundColor = control.Status switch
            {
                ControlTestStatus.Passed => XLColor.LightGreen,
                ControlTestStatus.Failed => XLColor.LightPink,
                ControlTestStatus.NotApplicable => XLColor.LightGray,
                ControlTestStatus.Deferred => XLColor.LightYellow,
                _ => XLColor.White
            };
            row++;

            if (!string.IsNullOrEmpty(control.RelatedSTIGIds))
            {
                ws.Cell(row, 1).Value = "STIG IDs:";
                ws.Cell(row, 2).Value = control.RelatedSTIGIds;
                row++;
            }

            ws.Cell(row, 1).Value = "Assessment Objective:";
            ws.Cell(row, 2).Value = control.AssessmentObjective;
            ws.Range(row, 2, row, 4).Merge();
            ws.Cell(row, 2).Style.Alignment.WrapText = true;
            row++;

            if (!string.IsNullOrEmpty(control.Comments))
            {
                ws.Cell(row, 1).Value = "Comments:";
                ws.Cell(row, 2).Value = control.Comments;
                ws.Range(row, 2, row, 4).Merge();
                row++;
            }

            ws.Cell(row, 1).Value = "Reviewer:";
            ws.Cell(row, 2).Value = control.ReviewerName;
            ws.Cell(row, 3).Value = "Date:";
            ws.Cell(row, 4).Value = control.ReviewDate?.ToString("MM/dd/yyyy") ?? "";
            row++;

            row++; // Blank row between controls
        }

        ws.Column(1).Width = 15;
        ws.Column(2).Width = 50;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 15;
    }

    #endregion

    private void AddHeaderRow(IXLWorksheet ws, int row, string label1, string value1, string label2, string value2)
    {
        ws.Cell(row, 1).Value = label1;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value1;
        ws.Cell(row, 3).Value = label2;
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = value2;
    }

    #endregion
}
