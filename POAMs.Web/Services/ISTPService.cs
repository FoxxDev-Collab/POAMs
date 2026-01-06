using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public interface ISTPService
{
    // CRUD for SecurityTestPlan
    Task<List<SecurityTestPlan>> GetAllAsync(STPType? type = null);
    Task<SecurityTestPlan?> GetByIdAsync(int id);
    Task<SecurityTestPlan?> GetByIdWithTestCasesAsync(int id);
    Task<List<SecurityTestPlan>> GetBySystemAsync(int systemId, STPType? type = null);
    Task<List<SecurityTestPlan>> GetByPOAMAsync(int poamId);
    Task<SecurityTestPlan> CreateAsync(SecurityTestPlan stp);
    Task<SecurityTestPlan> UpdateAsync(SecurityTestPlan stp);
    Task DeleteAsync(int id);

    // STIG Test Cases
    Task<STPTestCase?> GetSTIGTestCaseByIdAsync(int id);
    Task<STPTestCase> AddSTIGTestCaseAsync(STPTestCase testCase);
    Task<STPTestCase> UpdateSTIGTestCaseAsync(STPTestCase testCase);
    Task DeleteSTIGTestCaseAsync(int id);
    Task<List<STPTestCase>> AddSTIGTestCasesAsync(int stpId, List<STPTestCase> testCases);
    Task UpdateSTIGTestCaseStatusAsync(int testCaseId, TestCaseStatus status, string actualResult, string evidence, int? testerId);

    // Security Control Test Cases
    Task<SecurityControlTestCase?> GetControlTestCaseByIdAsync(int id);
    Task<SecurityControlTestCase> AddControlTestCaseAsync(SecurityControlTestCase testCase);
    Task<SecurityControlTestCase> UpdateControlTestCaseAsync(SecurityControlTestCase testCase);
    Task DeleteControlTestCaseAsync(int id);
    Task<List<SecurityControlTestCase>> AddControlTestCasesAsync(int stpId, List<SecurityControlTestCase> testCases);
    Task UpdateControlTestCaseStatusAsync(int testCaseId, ControlTestStatus status, string comments);

    // Nessus Test Cases
    Task<NessusTestCase?> GetNessusTestCaseByIdAsync(int id);
    Task<NessusTestCase> AddNessusTestCaseAsync(NessusTestCase testCase);
    Task<NessusTestCase> UpdateNessusTestCaseAsync(NessusTestCase testCase);
    Task DeleteNessusTestCaseAsync(int id);
    Task<List<NessusTestCase>> AddNessusTestCasesAsync(int stpId, List<NessusTestCase> testCases);
    Task UpdateNessusTestCaseStatusAsync(int testCaseId, VulnTestStatus status, string actualResult, string evidence, int? testerId);

    // Export - returns appropriate format based on STP type
    byte[] ExportToExcel(SecurityTestPlan stp);

    // Generate identifier
    Task<string> GenerateIdentifierAsync(int systemId, STPType type);
}
