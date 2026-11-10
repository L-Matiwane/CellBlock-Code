using System.Text.Json;
using CellBlock_Code.Models;

namespace CellBlock_Code.Services;

public class LeaderboardService
{
    private const int MaxEntries = 20;

    private readonly string _filePath =
        Path.Combine(FileSystem.AppDataDirectory, "leaderboard.json");

    // Used to highlight the score that was just saved
    public Guid? LastAddedId { get; private set; }

    public List<ScoreEntry> Load()
    {
        if (!File.Exists(_filePath)) return new();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? new();
        }
        catch { return new(); }
    }

    public bool QualifiesForTop20(int score)
    {
        var scores = Load();
        return score > 0 && (scores.Count < MaxEntries || score > scores.Min(s => s.Score));
    }

    public void Add(ScoreEntry entry)
    {
        var scores = Load();
        scores.Add(entry);

        var top = scores
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Date)
            .Take(MaxEntries)
            .ToList();

        File.WriteAllText(_filePath, JsonSerializer.Serialize(top));
        LastAddedId = entry.Id;
    }

    public void Clear()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
        LastAddedId = null;
    }
}