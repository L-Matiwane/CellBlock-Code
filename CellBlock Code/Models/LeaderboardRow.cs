namespace CellBlock_Code.Models;

public class LeaderboardRow
{
    public int Rank { get; init; }
    public string Username { get; init; } = "";
    public int Score { get; init; }
    public string Level { get; init; } = "";
    public string DateText { get; init; } = "";
    public Color LevelColor { get; init; } = Colors.White;
    public bool IsMine { get; init; }

    public string RankText => Rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"#{Rank}"
    };

    public Brush RowStroke => new SolidColorBrush(Color.FromArgb(IsMine ? "#FFB300" : "#37434B"));
}