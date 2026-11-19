using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Models;

namespace ItungoSoft.Farmer.Services;

public record ChartData(string[] TypeLabels, double[] TypeCounts, double[] Male, double[] Female,
    string[] MonthLabels, double[] MonthCounts, string[] AgeLabels, double[] AgeCounts);

public static class ChartService
{
    public static ChartData Load()
    {
        using var db = new AppDbContext();
        var fid = AuthService.Current!.Id;
        var today = DateTime.Today;

        var animals = db.Animals.Where(a => a.FarmerId == fid).ToList();
        var types = Enum.GetValues<AnimalType>();
        var typeCounts = types.Select(t => (double)animals.Count(a => a.Type == t)).ToArray();
        var male = types.Select(t => (double)animals.Count(a => a.Type == t && a.Gender == "Male")).ToArray();
        var female = types.Select(t => (double)animals.Count(a => a.Type == t && a.Gender == "Female")).ToArray();

        var first = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var dates = db.HealthRecords.Where(h => h.Animal!.FarmerId == fid && h.Date >= first).Select(h => h.Date).ToList();
        var starts = Enumerable.Range(0, 6).Select(i => first.AddMonths(i)).ToArray();
        var monthLabels = starts.Select(s => s.ToString("MMM yy")).ToArray();
        var monthCounts = starts.Select(s => (double)dates.Count(d => d >= s && d < s.AddMonths(1))).ToArray();

        var bins = new[] { ("0-6", 0, 6), ("7-12", 7, 12), ("13-24", 13, 24), ("25-48", 25, 48), ("49+", 49, int.MaxValue) };
        var ages = animals.Select(a => (today.Year - a.DateOfBirth.Year) * 12 + today.Month - a.DateOfBirth.Month).ToList();
        var ageCounts = bins.Select(b => (double)ages.Count(m => m >= b.Item2 && m <= b.Item3)).ToArray();

        return new ChartData(types.Select(t => t.ToString()).ToArray(), typeCounts, male, female,
            monthLabels, monthCounts, bins.Select(b => b.Item1).ToArray(), ageCounts);
    }
}