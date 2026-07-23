using System.Globalization;
using ClosedXML.Excel;

namespace PilotageFinancier.Application.Parsing;

/// <summary>Parseur CSV/Excel. Format retenu pour la V1 (import fichier unique, cf. document).</summary>
public class TabularParser : ITabularParser
{
    public IReadOnlyList<string[]> Lire(Stream flux, string nomFichier)
    {
        var ext = Path.GetExtension(nomFichier).ToLowerInvariant();
        return ext is ".xlsx" or ".xls" ? LireExcel(flux) : LireCsv(flux);
    }

    private static IReadOnlyList<string[]> LireCsv(Stream flux)
    {
        var lignes = new List<string[]>();
        using var reader = new StreamReader(flux);
        string? ligne;
        var premiere = true;
        while ((ligne = reader.ReadLine()) is not null)
        {
            if (premiere) { premiere = false; continue; } // saute l'en-tête
            if (string.IsNullOrWhiteSpace(ligne)) continue;
            var sep = ligne.Contains(';') ? ';' : ',';
            lignes.Add(ligne.Split(sep).Select(c => c.Trim().Trim('"')).ToArray());
        }
        return lignes;
    }

    private static IReadOnlyList<string[]> LireExcel(Stream flux)
    {
        var lignes = new List<string[]>();
        using var wb = new XLWorkbook(flux);
        var ws = wb.Worksheets.First();
        var premiere = true;
        foreach (var row in ws.RowsUsed())
        {
            if (premiere) { premiere = false; continue; }
            lignes.Add(row.Cells(1, row.LastCellUsed()?.Address.ColumnNumber ?? 1)
                          .Select(c => c.GetString().Trim()).ToArray());
        }
        return lignes;
    }

    public static decimal ParseDecimal(string s)
    {
        s = s.Replace(" ", "").Replace("\u00A0", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    }

    public static DateTime ParseDate(string s)
    {
        foreach (var f in new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy" })
            if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd) ? dd : default;
    }
}
