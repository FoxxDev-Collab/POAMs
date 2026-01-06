#r "nuget: ClosedXML, 0.102.1"

using ClosedXML.Excel;

var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("POA&M");

// Header Section
ws.Cell("D1").Value = "Plan of Action and Milestones (POA&M)";
ws.Cell("D1").Style.Font.Bold = true;
ws.Cell("D1").Style.Font.FontSize = 14;

ws.Cell("B2").Value = "System Name";
ws.Cell("C2").Value = "Test System Alpha";
ws.Cell("F2").Value = "Date of this POA&M";
ws.Cell("G2").Value = DateTime.Now;
ws.Cell("G2").Style.DateFormat.Format = "MM/dd/yyyy";

ws.Cell("B3").Value = "Company/Organization Name";
ws.Cell("C3").Value = "Test Organization";

// Column Headers (Row 12)
int headerRow = 12;
var headers = new[] {
    "Item Identifier",
    "Weakness or Deficiency",
    "Security Control",
    "POC",
    "Resources Required",
    "Scheduled Completion Date",
    "Milestones with Completion Dates",
    "Changes to Milestones",
    "Weakness/ Deficiency Identified by",
    "Risk Level\n(Low/Med/High)",
    "Estimated Cost",
    "Status",
    "Comments"
};

for (int i = 0; i < headers.Length; i++)
{
    var cell = ws.Cell(headerRow, i + 1);
    cell.Value = headers[i];
    cell.Style.Font.Bold = true;
    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
}

// Sample POAMs data
var poamData = new[] {
    ("26-O-0001", "Windows Server 2019 - Missing Critical Patches", "SI-2", "STIG Scan", "High", "Ongoing"),
    ("26-O-0002", "Firewall Configuration Non-Compliant", "SC-7", "Security Audit", "High", "Open"),
    ("26-O-0003", "User Account Management Deficiency", "AC-2", "Internal Review", "Moderate", "Open"),
    ("26-O-0004", "Audit Log Retention Below Requirements", "AU-11", "Compliance Check", "Moderate", "Ongoing"),
    ("26-O-0005", "Password Complexity Not Enforced", "IA-5", "STIG Scan", "High", "Open"),
    ("26-O-0006", "Antivirus Definitions Outdated", "SI-3", "Automated Scan", "High", "Completed"),
    ("26-O-0007", "Session Timeout Not Configured", "AC-12", "Penetration Test", "Low", "Open"),
    ("26-O-0008", "SSH Keys Not Rotated", "IA-5", "Security Assessment", "Moderate", "Ongoing"),
    ("26-O-0009", "Network Segmentation Insufficient", "SC-7", "Architecture Review", "High", "Open"),
    ("26-O-0010", "Database Encryption Not Enabled", "SC-28", "Data Assessment", "High", "Open"),
    ("26-O-0011", "Backup Verification Not Performed", "CP-9", "BCP Review", "Moderate", "Ongoing"),
    ("26-O-0012", "MFA Not Implemented for Admin Accounts", "IA-2", "Security Audit", "High", "Open"),
    ("26-O-0013", "Vulnerability Scan Coverage Incomplete", "RA-5", "Assessment Review", "Moderate", "Open"),
    ("26-O-0014", "Incident Response Plan Outdated", "IR-8", "Annual Review", "Low", "Draft"),
    ("26-O-0015", "Certificate Expiration Monitoring Missing", "SC-17", "Infrastructure Audit", "Moderate", "Open")
};

int dataRow = headerRow + 1;
var random = new Random(42);

foreach (var (itemId, weakness, control, source, risk, status) in poamData)
{
    ws.Cell(dataRow, 1).Value = itemId;
    ws.Cell(dataRow, 2).Value = weakness;
    ws.Cell(dataRow, 3).Value = control;
    ws.Cell(dataRow, 4).Value = "Admin User"; // POC
    ws.Cell(dataRow, 5).Value = "System Administrators, Security Team";

    var completionDate = DateTime.Now.AddDays(random.Next(30, 180));
    ws.Cell(dataRow, 6).Value = completionDate;
    ws.Cell(dataRow, 6).Style.DateFormat.Format = "MM/dd/yyyy";

    // Two milestones per POAM
    var ms1Date = DateTime.Now.AddDays(random.Next(15, 60));
    var ms2Date = completionDate.AddDays(-14);
    ws.Cell(dataRow, 7).Value = $"1. Develop remediation plan = {ms1Date:MM/dd/yyyy}\n2. Implement and verify fix = {ms2Date:MM/dd/yyyy}";
    ws.Cell(dataRow, 7).Style.Alignment.WrapText = true;

    ws.Cell(dataRow, 8).Value = ""; // Changes
    ws.Cell(dataRow, 9).Value = source;
    ws.Cell(dataRow, 10).Value = risk;
    ws.Cell(dataRow, 11).Value = random.Next(500, 5000);
    ws.Cell(dataRow, 11).Style.NumberFormat.Format = "$#,##0";
    ws.Cell(dataRow, 12).Value = status;
    ws.Cell(dataRow, 13).Value = $"Test POAM for {control} control testing";

    // Apply borders
    for (int col = 1; col <= 13; col++)
    {
        ws.Cell(dataRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    dataRow++;
}

// Adjust columns
ws.Columns().AdjustToContents();
ws.Column(2).Width = 40;
ws.Column(7).Width = 45;

var outputPath = @"c:\Users\jeremiah.price\Documents\3. Local-Dev\Compliance_Tools\POAMs\TestPOAMs_15_Items.xlsx";
workbook.SaveAs(outputPath);
Console.WriteLine($"Created: {outputPath}");
