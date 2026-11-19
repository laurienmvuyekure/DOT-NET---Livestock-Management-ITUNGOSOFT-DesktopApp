using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ItungoSoft.Farmer.Data;

namespace ItungoSoft.Farmer.Services;

public static class ExportService
{
    static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    static int Fid => AuthService.Current!.Id;

    public static void ExportCsv(string path)
    {
        using var db = new AppDbContext(); var fid = Fid;
        var sb = new StringBuilder("Animals\nEarTag,Type,Breed,Gender,DateOfBirth\n");
        foreach (var a in db.Animals.Where(a => a.FarmerId == fid).OrderBy(a => a.EarTag).ToList())
            sb.AppendLine(string.Join(",", Q(a.EarTag), a.Type, Q(a.Breed), a.Gender, a.DateOfBirth.ToString("yyyy-MM-dd")));
        sb.AppendLine().AppendLine("Health\nEarTag,Type,Description,Date,Status");
        foreach (var h in db.HealthRecords.Include(h => h.Animal).Where(h => h.Animal!.FarmerId == fid).OrderBy(h => h.Date).ToList())
            sb.AppendLine(string.Join(",", Q(h.Animal!.EarTag), h.Kind, Q(h.Description), h.Date.ToString("yyyy-MM-dd"), h.Status));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    public static void PopulationPdf(string path)
    {
        using var db = new AppDbContext(); var fid = Fid;
        var rows = db.Animals.Where(a => a.FarmerId == fid).ToList().GroupBy(a => a.Type).OrderBy(g => g.Key.ToString())
            .Select(g => new[] { g.Key.ToString(), g.Count().ToString(), g.Count(a => a.Gender == "Male").ToString(), g.Count(a => a.Gender == "Female").ToString() }).ToList();
        Pdf(path, "Animal population report", new[] { "Type", "Total", "Male", "Female" }, rows);
    }

    public static void HealthPdf(string path)
    {
        using var db = new AppDbContext(); var fid = Fid;
        var rows = db.HealthRecords.Include(h => h.Animal).Where(h => h.Animal!.FarmerId == fid).OrderBy(h => h.Date).ToList()
            .Select(h => new[] { h.Animal!.EarTag, h.Kind.ToString(), h.Description, h.Date.ToString("dd/MM/yyyy"), h.Status }).ToList();
        Pdf(path, "Health summary report", new[] { "Animal", "Type", "Description", "Date", "Status" }, rows);
    }

    public static void BreedingPdf(string path)
    {
        using var db = new AppDbContext(); var fid = Fid;
        var rows = db.BreedingRecords.Include(b => b.Animal).Where(b => b.Animal!.FarmerId == fid).OrderBy(b => b.MatingDate).ToList()
            .Select(b => new[] { b.Animal!.EarTag, b.SireEarTag, b.Method, b.MatingDate.ToString("dd/MM/yyyy"), b.ExpectedDelivery.ToString("dd/MM/yyyy"), b.Status, b.Outcome ?? "" }).ToList();
        Pdf(path, "Breeding performance report", new[] { "Female", "Male", "Method", "Mated", "Expected", "Status", "Outcome" }, rows);
    }

    static void Pdf(string path, string title, string[] headers, List<string[]> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(c => c.Page(p =>
        {
            p.Margin(30);
            p.Header().Text(title).FontSize(18).Bold();
            p.Content().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(cd => { foreach (var x in headers) cd.RelativeColumn(); });
                t.Header(h => { foreach (var x in headers) h.Cell().Background("#E8F5E9").Padding(4).Text(x).Bold(); });
                foreach (var r in rows) foreach (var v in r) t.Cell().BorderBottom(0.5f).Padding(4).Text(v);
            });
            p.Footer().AlignRight().Text($"ITUNGOSOFT - generated {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
        })).GeneratePdf(path);
    }

    // Backup / restore use PostgreSQL's pg_dump and pg_restore. Returns null on success, else an error message.
    public static string? Backup(string file) => RunPg("pg_dump", $"-Fc -f \"{file}\" {Db.Database}");
    public static string? Restore(string file) => RunPg("pg_restore", $"--clean --if-exists -d {Db.Database} \"{file}\"");
    static NpgsqlConnectionStringBuilder Db => new(AppDbContext.ConnectionString);

    static string? RunPg(string exe, string args)
    {
        var b = Db;
        var root = @"C:\Program Files\PostgreSQL";
        var found = Directory.Exists(root)
            ? Directory.GetDirectories(root).OrderByDescending(d => d).Select(d => Path.Combine(d, "bin", exe + ".exe")).FirstOrDefault(File.Exists)
            : null;
        var psi = new ProcessStartInfo(found ?? exe, $"-h {b.Host} -p {b.Port} -U {b.Username} " + args)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        psi.Environment["PGPASSWORD"] = b.Password ?? "";
        try
        {
            using var p = Process.Start(psi)!;
            var err = p.StandardError.ReadToEnd(); p.WaitForExit();
            return p.ExitCode == 0 ? null : err;
        }
        catch (Exception ex) { return $"Could not run {exe}: {ex.Message}"; }
    }
}
