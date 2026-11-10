using CellBlock_Code.Models;
using CellBlock_Code.Services;

namespace CellBlock_Code.Views;

public partial class StartPage : ContentPage
{
    private readonly GameEngine _engine;

    public StartPage(GameEngine engine)
    {
        InitializeComponent();
        _engine = engine;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PlayLockedIntro();
    }

    private bool _introRunning;

    private async Task PlayLockedIntro()
    {
        if (_introRunning) return;
        _introRunning = true;

        try
        {
            // Reset so the animation can replay every time
            PrisonImage.Opacity = 0;
            PrisonImage.Scale = 1.25;
            FlashOverlay.Opacity = 0;
            LockLabel.Opacity = 0;
            LockLabel.Scale = 4;
            StatusCard.Opacity = 0;
            DifficultyPanel.Opacity = 0;
            DifficultyPanel.InputTransparent = true;

            // 1. Camera pulls back onto the cell
            await Task.WhenAll(
                PrisonImage.FadeTo(1, 900),
                PrisonImage.ScaleTo(1, 1400, Easing.CubicOut));

            // 2. The lock slams onto the screen
            await Task.WhenAll(
                LockLabel.FadeTo(1, 150),
                LockLabel.ScaleTo(1, 400, Easing.BounceOut));

            // 3. Alarm flash, twice
            for (int i = 0; i < 2; i++)
            {
                await FlashOverlay.FadeTo(0.35, 90);
                await FlashOverlay.FadeTo(0, 220);
            }

            // 4. Status card, then the level buttons fade in
            await StatusCard.FadeTo(1, 400);
            await DifficultyPanel.FadeTo(1, 500);

            DifficultyPanel.InputTransparent = false;
        }
        finally
        {
            _introRunning = false;
        }
    }

    private async void OnDifficultyClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var difficulty = Enum.Parse<Difficulty>(button.ClassId);

        await button.ScaleTo(0.92, 60);
        await button.ScaleTo(1, 60);

        _engine.NewGame(difficulty);
        await Shell.Current.GoToAsync("//GamePage");
    }

    private async void OnHowToPlayClicked(object? sender, EventArgs e)
    {
        await HowToPlay.ShowAsync();
    }

    private async void OnLeaderboardClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LeaderboardPage");
    }
}