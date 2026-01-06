using ClosedXML.Excel;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public class XactaExportService : IExportService
{
    public byte[] ExportToXacta(IEnumerable<POAM> poams, SystemInfo system)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("POA&M");

        // Header Section
        worksheet.Cell("D1").Value = "Plan of Action and Milestones (POA&M)";
        worksheet.Cell("D1").Style.Font.Bold = true;
        worksheet.Cell("D1").Style.Font.FontSize = 14;

        worksheet.Cell("B2").Value = "System Name";
        worksheet.Cell("C2").Value = system.SystemName;
        worksheet.Cell("F2").Value = "Date of this POA&M";
        worksheet.Cell("G2").Value = DateTime.UtcNow;
        worksheet.Cell("G2").Style.DateFormat.Format = "MM/dd/yyyy";

        worksheet.Cell("B3").Value = "Company/Organization Name";
        worksheet.Cell("C3").Value = system.OrganizationName;
        worksheet.Cell("F3").Value = "Date of Last Update";
        worksheet.Cell("G3").Value = DateTime.UtcNow;
        worksheet.Cell("G3").Style.DateFormat.Format = "MM/dd/yyyy";

        worksheet.Cell("F4").Value = "Date of Original POA&M";
        var firstPoam = poams.FirstOrDefault();
        if (firstPoam != null)
        {
            worksheet.Cell("G4").Value = firstPoam.OriginalPOAMDate;
            worksheet.Cell("G4").Style.DateFormat.Format = "MM/dd/yyyy";
        }

        // ISSM Information Section
        worksheet.Cell("A6").Value = "ISSM Information";
        worksheet.Cell("A6").Style.Font.Bold = true;

        worksheet.Cell("B7").Value = "Name";
        worksheet.Cell("C7").Value = system.ISSM?.DisplayName ?? "";
        worksheet.Cell("F7").Value = "IS Type";
        worksheet.Cell("G7").Value = system.ISType ?? "";

        worksheet.Cell("B8").Value = "Phone";
        worksheet.Cell("C8").Value = system.ISSM?.Phone ?? "";
        worksheet.Cell("F8").Value = "UID";
        worksheet.Cell("G8").Value = system.UID ?? "";

        worksheet.Cell("B9").Value = "Email";
        worksheet.Cell("C9").Value = system.ISSM?.Email ?? "";

        // Column Headers (Row 12)
        int headerRow = 12;
        var headers = new[]
        {
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
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        // Data Rows
        int dataRow = headerRow + 1;
        foreach (var poam in poams)
        {
            worksheet.Cell(dataRow, 1).Value = poam.ItemIdentifier;
            worksheet.Cell(dataRow, 2).Value = poam.WeaknessOrDeficiency;
            worksheet.Cell(dataRow, 3).Value = poam.SecurityControl ?? "";
            worksheet.Cell(dataRow, 4).Value = poam.POC?.DisplayName ?? "";
            worksheet.Cell(dataRow, 5).Value = poam.ResourcesRequired ?? "";

            worksheet.Cell(dataRow, 6).Value = poam.ScheduledCompletionDate;
            worksheet.Cell(dataRow, 6).Style.DateFormat.Format = "MM/dd/yyyy";

            // Format milestones
            var milestones = poam.Milestones.OrderBy(m => m.MilestoneNumber)
                .Select(m => $"{m.MilestoneNumber}. {m.Title} = {m.DueDate:MM/dd/yyyy}");
            worksheet.Cell(dataRow, 7).Value = string.Join("\n", milestones);
            worksheet.Cell(dataRow, 7).Style.Alignment.WrapText = true;

            // Changes to milestones
            var changes = poam.Milestones
                .Where(m => !string.IsNullOrEmpty(m.Changes))
                .Select(m => m.Changes);
            worksheet.Cell(dataRow, 8).Value = string.Join("\n", changes);
            worksheet.Cell(dataRow, 8).Style.Alignment.WrapText = true;

            worksheet.Cell(dataRow, 9).Value = poam.IdentifiedBy ?? "";

            var riskText = poam.RiskLevel switch
            {
                RiskLevel.Low => "Low",
                RiskLevel.Moderate => "Med",
                RiskLevel.High => "High",
                _ => ""
            };
            worksheet.Cell(dataRow, 10).Value = riskText;

            worksheet.Cell(dataRow, 11).Value = poam.EstimatedCost ?? 0;
            worksheet.Cell(dataRow, 11).Style.NumberFormat.Format = "$#,##0.00";

            worksheet.Cell(dataRow, 12).Value = poam.Status.ToString();
            worksheet.Cell(dataRow, 13).Value = poam.Comments ?? "";

            // Apply borders to data row
            for (int col = 1; col <= 13; col++)
            {
                worksheet.Cell(dataRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            dataRow++;
        }

        // Auto-fit columns with max width
        worksheet.Columns().AdjustToContents(1, dataRow);
        foreach (var column in worksheet.Columns())
        {
            if (column.Width > 50)
                column.Width = 50;
        }

        // Set specific column widths for better readability
        worksheet.Column(2).Width = 40; // Weakness
        worksheet.Column(7).Width = 35; // Milestones

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportSingleToXacta(POAM poam)
    {
        return ExportToXacta(new[] { poam }, poam.System);
    }
}
