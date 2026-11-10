using CellBlock_Code.Models;

namespace CellBlock_Code.Services;

public class GameEngine
{
    private readonly Random _rng = new();
    private int[] _secret = new int[3];

    public Difficulty Difficulty { get; private set; }
    public DifficultySettings Settings { get; private set; } = DifficultySettings.For(Difficulty.Easy);
    public int GuessCount { get; private set; }

    public void NewGame(Difficulty difficulty)
    {
        Difficulty = difficulty;
        Settings = DifficultySettings.For(difficulty);
        GuessCount = 0;

        // 3 different digits: shuffle 0-9 and take the first three
        _secret = Enumerable.Range(0, 10).OrderBy(_ => _rng.Next()).Take(3).ToArray();
    }

    public bool IsValidGuess(string guess) =>
        guess.Length == 3 && guess.All(char.IsDigit) && guess.Distinct().Count() == 3;

    public GuessResult Evaluate(string guess)
    {
        GuessCount++;
        int hits = 0, matches = 0;

        for (int i = 0; i < 3; i++)
        {
            int digit = guess[i] - '0';
            if (digit == _secret[i]) hits++;
            else if (_secret.Contains(digit)) matches++;
        }
        return new GuessResult(guess, hits, matches);
    }

    public int CalculateScore(int secondsLeft)
    {
        // The time bonus is a share of the level's time limit (max 500 points)
        double timeBonus = 500.0 * secondsLeft / Settings.TimeLimitSeconds;
        double raw = 1000 - (GuessCount * 50) + timeBonus;
        return (int)Math.Max(0, raw * Settings.ScoreMultiplier);
    }

    // TESTING ONLY: shows the secret code. DELETE THIS LINE BEFORE SUBMITTING.
    public string CurrentSecretForTesting => string.Concat(_secret);
}