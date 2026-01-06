#r "nuget: ClosedXML, 0.102.1"

using ClosedXML.Excel;

var templatePath = @"c:\Users\jeremiah.price\Documents\3. Local-Dev\Compliance_Tools\POAMs\stp_guidance\STIG_STP_Template.xlsx";
var wb = new XLWorkbook(templatePath);

foreach (var ws in wb.Worksheets)
{
    Console.WriteLine($"\n=== Sheet: {ws.Name} ===");
    var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
    var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

    Console.WriteLine($"Dimensions: {lastRow} rows x {lastCol} columns\n");

    for (int r = 1; r <= Math.Min(30, lastRow); r++)
    {
        var rowData = new List<string>();
        for (int c = 1; c <= lastCol; c++)
        {
            var val = ws.Cell(r, c).GetString();
            if (!string.IsNullOrWhiteSpace(val))
            {
                var colLetter = GetColumnLetter(c);
                rowData.Add($"{colLetter}:{val.Replace("\n", "\\n").Substring(0, Math.Min(50, val.Length))}");
            }
        }
        if (rowData.Any())
            Console.WriteLine($"Row {r}: {string.Join(" | ", rowData)}");
    }
}

string GetColumnLetter(int col)
{
    string result = "";
    while (col > 0)
    {
        col--;
        result = (char)('A' + col % 26) + result;
        col /= 26;
    }
    return result;
}
