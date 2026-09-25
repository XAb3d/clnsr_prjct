using System.Globalization;
using System.Reflection;
using System.Text;
using CleanserBlazorUI.Converters;
using CleanserBlazorUI.Data;
using CleanserBlazorUI.Entities;

namespace CleanserBlazorUI.Engine;

/// <summary>
/// Maps named CSV columns onto IND/BUS transformers, then row-level ISCleaned.
/// </summary>
public class CleanserEngine
{
    readonly DataManagementService _data;
    readonly SemaphoreSlim _gate = new(1, 1);
    IsCleanedEvaluator? _evaluator;

    public CleanserEngine(DataManagementService data)
    {
        _data = data;
    }

    public async Task<EvaluateResponse> EvaluateCsvAsync(string recordType, Stream csvStream, CancellationToken ct = default)
    {
        var type = NormalizeRecordType(recordType);
        await EnsureEvaluatorAsync(ct);
        var table = ReadCsv(csvStream);
        if (table.Headers.Count == 0)
        {
            return new EvaluateResponse { RecordType = type, RowCount = 0, UnlCount = 0 };
        }

        var rows = new List<RowEval>(table.Rows.Count);
        if (type == "bus")
        {
            var xf = new BusinessDataTransformer();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var ctx = new BusinessContext();
                Fill(ctx, xf, table, i);
                var passed = _evaluator!.Evaluate(ctx);
                rows.Add(new RowEval(RowIndex(table, i), !passed, CollectReasons(ctx)));
            }
        }
        else
        {
            var xf = new IndividualDataTransformer();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var ctx = new IndividualContext();
                Fill(ctx, xf, table, i);
                var passed = _evaluator!.Evaluate(ctx);
                rows.Add(new RowEval(RowIndex(table, i), !passed, CollectReasons(ctx)));
            }
        }

        return new EvaluateResponse
        {
            RecordType = type,
            RowCount = rows.Count,
            UnlCount = rows.Count(r => r.IsUnl),
            Rows = rows,
        };
    }

    static string NormalizeRecordType(string recordType)
    {
        var t = (recordType ?? "").Trim().ToLowerInvariant();
        if (t is "ind" or "individual" or "idu") return "ind";
        if (t is "bus" or "business" or "bdu") return "bus";
        throw new ArgumentException("recordType must be 'ind' or 'bus'.");
    }

    async Task EnsureEvaluatorAsync(CancellationToken ct)
    {
        if (_evaluator != null) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (_evaluator != null) return;
            List<string> keywords;
            Dictionary<string, string> shortForms;
            try
            {
                keywords = await _data.InitializeSettingsDBAsync();
                shortForms = await _data.BUS_NUM_InitializeSettingsDBAsyncNormalizer_Home();
            }
            catch
            {
                keywords = new List<string>();
                shortForms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _evaluator = new IsCleanedEvaluator(keywords, shortForms);
        }
        finally
        {
            _gate.Release();
        }
    }

    static int RowIndex(CsvTable table, int i)
    {
        var raw = Get(table, i, "RowIndex") ?? Get(table, i, "row_index");
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return i;
    }

    static void Fill<TContext, TXf>(TContext context, TXf transformer, CsvTable table, int rowIndex)
        where TContext : class
        where TXf : class
    {
        var xfType = transformer.GetType();
        foreach (var prop in typeof(TContext).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;
            var value = Get(table, rowIndex, prop.Name);
            if (value == null && prop.Name.Equals("Facilityaccnum", StringComparison.OrdinalIgnoreCase))
                value = Get(table, rowIndex, "Credit FacilityAccNum", "FacilityAccNum");
            value ??= "";
            var method = xfType.GetMethod(prop.Name, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                if (prop.PropertyType == typeof(string))
                    prop.SetValue(context, value);
                continue;
            }
            var parms = method.GetParameters();
            object? result;
            try
            {
                if (parms.Length == 0)
                    result = method.Invoke(transformer, null);
                else if (parms.Length == 1)
                    result = method.Invoke(transformer, new object[] { value });
                else if (method.Name.Equals("DateRestructured", StringComparison.OrdinalIgnoreCase))
                    result = method.Invoke(transformer, new object[] { value, Get(table, rowIndex, "FacilityStatusCode") ?? "" });
                else if (method.Name.Equals("DateOfEmp", StringComparison.OrdinalIgnoreCase))
                    result = method.Invoke(transformer, new object[] { value, "DateOfEmp" });
                else
                    result = method.Invoke(transformer, new object[] { value, "" });
            }
            catch
            {
                continue;
            }
            if (result != null && prop.PropertyType.IsInstanceOfType(result))
                prop.SetValue(context, result);
            else if (prop.PropertyType == typeof(string) && result is string s)
                prop.SetValue(context, s);
        }
    }

    static string CollectReasons(object record)
    {
        var reasons = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var prop in record.GetType().GetProperties())
        {
            if (prop.PropertyType != typeof(CellDataAndStatus)) continue;
            if (prop.GetValue(record) is not CellDataAndStatus cell) continue;
            if (cell.Passed || cell.Errors == null) continue;
            foreach (var e in cell.Errors)
            {
                if (!string.IsNullOrWhiteSpace(e)) reasons.Add(e.Trim());
            }
        }
        return string.Join("; ", reasons);
    }

    static string? Get(CsvTable table, int rowIndex, params string[] names)
    {
        if (rowIndex < 0 || rowIndex >= table.Rows.Count) return null;
        var row = table.Rows[rowIndex];
        foreach (var name in names)
        {
            var key = NormHeader(name);
            if (table.Index.TryGetValue(key, out var col) && col < row.Count)
                return row[col];
        }
        return null;
    }

    static string NormHeader(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    sealed class CsvTable
    {
        public List<string> Headers { get; } = new();
        public Dictionary<string, int> Index { get; } = new(StringComparer.Ordinal);
        public List<List<string>> Rows { get; } = new();
    }

    static CsvTable ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var table = new CsvTable();
        string? headerLine = reader.ReadLine();
        if (headerLine == null) return table;
        table.Headers.AddRange(ParseCsvLine(headerLine));
        for (int i = 0; i < table.Headers.Count; i++)
        {
            var key = NormHeader(table.Headers[i]);
            if (!table.Index.ContainsKey(key)) table.Index[key] = i;
        }
        // CKMS IND facility column is "Credit FacilityAccNum"
        if (!table.Index.ContainsKey("creditfacilityaccnum"))
        {
            for (int i = 0; i < table.Headers.Count; i++)
            {
                if (NormHeader(table.Headers[i]) == "creditfacilityaccnum")
                    table.Index["creditfacilityaccnum"] = i;
            }
        }
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            table.Rows.Add(ParseCsvLine(line));
        }
        return table;
    }

    static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var cur = new StringBuilder();
        var inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cur.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else cur.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',')
            {
                fields.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(c);
        }
        fields.Add(cur.ToString());
        return fields;
    }
}
