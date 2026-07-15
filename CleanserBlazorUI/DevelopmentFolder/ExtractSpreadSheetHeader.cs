using CleanserBlazorUI.Helpers;

namespace CleanserBlazorUI.DevelopmentFolder;
public class ExtractSpreadSheetHeader
{
    //ws.Cell(correctDataCellIndex, 1).Value = rows[i].Data;
    string met = string.Empty;
    public static void GenerateMethod()
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.Individual)
        {
           hh += $"individualContext.{item} = {item}(row.Cell({num}).GetString()); \n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateCorrectSaved()
    {
        //wsu.Cell(wrongDataCellIndex, 7).Value = rows[i].Prevregnum ?? string.Empty;
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.Business)
        {
            hh += $" ws.Cell(correctDataCellIndex, {num}).Value = rows[i].{item} ?? string.Empty; \n\n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateCorrectSaved1()
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.Individual)
        {
            hh += $"wsu.Cell(wrongDataCellIndex, {num}).Value = rows[i].{item} ?? string.Empty; \n\n";
            num++;
        }
        int pp = 0;
    }
    //{ return new CellDataAndStatus(data); }
    public static void GenerateCorrectClass()
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.Individual)
        {
            hh += $"private static CellDataAndStatus {item}(string data) gfgfgfg \n\n";
            num++;
        }
        int pp = 0;
    }
    
    public static void HeaderExtracter(Stream _file)
    {

        // Open the workbook
        using (var workbook = new XLWorkbook(_file))
        {
            // Access the first worksheet
            var worksheet = workbook.Worksheet(1);

            // Get the first row (headers)
            var headers = worksheet.Row(1)
                                    .CellsUsed()
                                    .Select(cell => cell.GetValue<string>())
                                    .Select(header => header.Replace(" ", "").Trim())
                                    .ToList();

            // Print the cleaned headers
            headers.ForEach(Console.WriteLine);
            string dd = "";
            foreach (var cell in headers)
            {
                dd += $"\"{cell}\",";
            }
            int bb = 0;
        }
    }
    public static void CreateAClassForExtractingData(string data)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.Individual)
        {
            hh += $"individualContext.{item} = {item}(row.Cell({num}).GetString()); \n";
            num++;
        }
        int pp = 0;
    }//public CellDataAndStatus? Busregnum { get; set; } = new CellDataAndStatus(string.Empty);
    public static void GenerateEntityClass()
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in SpreadSheetHeadersData.BusinessDud)
        {
            hh += $"public CellDataAndStatus {item} (string data)\r\n  {{\r\n  var cellData = new CellDataAndStatus(data);\r\n  data = stringHelper.RemoveSystemErroNames(data);\r\n  cellData.Data = data.ToUpper();\r\n  return cellData;\r\n    }} \n";
            num++;
        }
        int pp = 0;
    }
    public static void ReadAllRowFromExcel(List<string> spreadSheetHeadersData)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in spreadSheetHeadersData)
        {
            hh += $"{item} = indTransformerDub.{item}(row.Cell({num}).GetString()), \n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateContext(List<string> spreadSheetHeadersData)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in spreadSheetHeadersData)
        {
            hh += $"public CellDataAndStatus? {item} {{ get; set; }} = new CellDataAndStatus(string.Empty); \n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateTransformer(List<string> spreadSheetHeadersData)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in spreadSheetHeadersData)
        {
            hh += $"public CellDataAndStatus {item}(string data)\r\n    {{\r\n        var cellData = new CellDataAndStatus(data);\r\n        data = stringHelper.RemoveSystemErroNames(data);\r\n        cellData.Data = data.ToUpper();\r\n        return cellData;\r\n    }} \n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateWriteRowData(List<string> spreadSheetHeadersData)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in spreadSheetHeadersData)
        {
            hh += $"ws.Cell(rowIndex, {num}).Value = businessRowDud.{item}.Data; \n";
            num++;
        }
        int pp = 0;
    }
    public static void GenerateWriteRowOldData(List<string> spreadSheetHeadersData)
    {
        string hh = string.Empty;
        int num = 1;
        foreach (var item in spreadSheetHeadersData)
        {
            hh += $"ws.Cell(rowIndex, {num}).Value = businessRowDud.{item}.OldData; \n";
            num++;
        }
        int pp = 0;
    }
}

