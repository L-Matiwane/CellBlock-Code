namespace CellBlock_Code.Models;

public class DifficultySettings
{
    public int TimeLimitSeconds { get; init; }
    public double ScoreMultiplier { get; init; }

    // How many past guesses to show: 0 = none, int.MaxValue = all
    public int HistoryLimit { get; init; }
    public bool ShowHistory => HistoryLimit > 0;

    public static DifficultySettings For(Difficulty d) => d switch
    {
        Difficulty.Easy => new() { TimeLimitSeconds = 480, ScoreMultiplier = 1.0, HistoryLimit = int.MaxValue },
        Difficulty.Medium => new() { TimeLimitSeconds = 300, ScoreMultiplier = 1.5, HistoryLimit = 5 },
        _ => new() { TimeLimitSeconds = 120, ScoreMultiplier = 2.0, HistoryLimit = 0 },
    };
}