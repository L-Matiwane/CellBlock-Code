using CellBlock_Code.Models;
using CellBlock_Code.Services;

namespace CellBlock_Code.Views;

public partial class LeaderboardPage : ContentPage
{
    private readonly LeaderboardService _leaderboard;

    public LeaderboardPage(LeaderboardService leaderboard)
    {
        InitializeComponent();
        _leaderboard = leaderboard;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadScores();
    }

    private void LoadScores()
    {
        var rows = _leaderboard.Load()
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Date)
            .Select((s, i) => new LeaderboardRow
            {
                Rank = i + 1,
                Username = s.Username,
                Score = s.Score,
                Level = s.Difficulty.ToString().ToUpper(),
                DateText = s.Date.ToString("dd MMM yyyy"),
                LevelColor = s.Difficulty switch
                {
                    Difficulty.Easy => Color.FromArgb("#4ADE80"),
                    Difficulty.Medium => Color.FromArgb("#FF8B47"),
                    _ => Color.FromArgb("#FF5C5C")
                },
                IsMine = s.Id == _leaderboard.LastAddedId
            })
            .ToList();

        ScoreList.ItemsSource = rows;
    }

    private async void Back_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//StartPage");
    }

    private async void Clear_Clicked(object? sender, EventArgs e)
    {
        bool yes = await DisplayAlert("Clear leaderboard",
            "Delete all saved scores?", "Delete", "Cancel");
        if (!yes) return;

        _leaderboard.Clear();
        LoadScores();
    }
}