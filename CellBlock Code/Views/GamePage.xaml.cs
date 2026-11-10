using System.Collections.ObjectModel;
using CellBlock_Code.Models;
using CellBlock_Code.Services;

namespace CellBlock_Code.Views;

public partial class GamePage : ContentPage
{
    private static readonly Color Orange = Color.FromArgb("#FF6B1A");
    private static readonly Color Soft = Color.FromArgb("#FF8B47");
    private static readonly Color Slate = Color.FromArgb("#37434B");
    private static readonly Color DisplayDark = Color.FromArgb("#111111");
    private static readonly Color Metal = Color.FromArgb("#A9ACAE");

    // Your prisoner image in Resources\Images (lowercase, no spaces). Change this to your file name.
    private const string RunnerImage = "prisoner_running.png";
    private const double RunnerStartScale = 0.85;   // bigger number = bigger prisoner
    private const bool RunnerFacesRight = true;     // set to false if he runs backwards
    private readonly GameEngine _engine;
    private readonly LeaderboardService _leaderboard;
    private readonly IDispatcherTimer _timer;
    private readonly ObservableCollection<GuessResult> _history = new();

    private string _current = "";
    private int _secondsLeft;
    private int _score;
    private bool _finished;
    private bool _busy;

    public GamePage(GameEngine engine, LeaderboardService leaderboard)
    {
        InitializeComponent();
        _engine = engine;
        _leaderboard = leaderboard;

        // Feed the horizontal history strip
        BindableLayout.SetItemsSource(HistoryPanel, _history);

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;

        HowToPlay.Closed += OnHelpClosed;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartRound();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    // ---------- Round setup ----------

    private void StartRound()
    {
        _current = "";
        _finished = false;
        _busy = false;
        _history.Clear();
        _secondsLeft = _engine.Settings.TimeLimitSeconds;

        GuessesLabel.Text = "0";
        HitsLabel.Text = "0";
        MatchesLabel.Text = "0";
        UpdateTimerLabel();
        UpdateCodeDisplay();
        SetTerminal(null);
        SetStatus("🔒", "CELL LOCKED", Orange,
            $"{_engine.Difficulty.ToString().ToUpper()} level • find the secret three-digit code.");
        ShowMessage("ENTER THE ESCAPE CODE");

        // Hide the escape scene and put it back to the locked cell
        EscapeScene.IsVisible = false;
        EscapeScene.Opacity = 0;
        PrisonImage.Source = "prison_locked.png";
        PrisonImage.Opacity = 1;
        PrisonerImage.IsVisible = false;
        PrisonerImage.Opacity = 0;
        PrisonerImage.Scale = 1;
        PrisonerImage.TranslationY = 0;
        PrisonerImage.Rotation = 0;
        SceneCaption.Text = "";

        EnterButton.IsEnabled = true;
        EnterButton.BackgroundColor = Color.FromArgb("#A8460A");
        EnterButton.BorderColor = Color.FromArgb("#FF8B3D");

        // Past combinations: all on Easy, last 5 on Medium, none on Hard
        int historyLimit = _engine.Settings.HistoryLimit;
        HistorySection.IsVisible = _engine.Settings.ShowHistory;
        HistoryTitle.Text = historyLimit == int.MaxValue
            ? "PAST COMBINATIONS  •  SWIPE →"
            : $"LAST {historyLimit} COMBINATIONS  •  SWIPE →";

        SavePanel.IsVisible = false;
        ResultPanel.IsVisible = false;
        ResultPanel.Opacity = 0;

        _timer.Start();
    }

    // ---------- Timer ----------

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (_finished) return;

        _secondsLeft--;
        UpdateTimerLabel();

        if (_secondsLeft <= 0)
            await TimeUpSequence();
    }

    private void UpdateTimerLabel()
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, _secondsLeft));
        TimerLabel.Text = $"{t.Minutes:00}:{t.Seconds:00}";
        TimerLabel.TextColor = _secondsLeft <= 10 ? Colors.Red : Colors.White;
    }

    // ---------- Keypad ----------

    private async void Digit_Clicked(object? sender, EventArgs e)
    {
        if (_finished || _busy || sender is not Button button) return;

        await button.ScaleTo(0.88, 60);
        await button.ScaleTo(1, 60);

        if (_current.Length >= 3) return;

        if (_current.Contains(button.Text))
        {
            ShowMessage("DIGIT ALREADY ENTERED", Colors.OrangeRed);
            await ShakeView(CodeDisplay, 7);
            return;
        }

        _current += button.Text;
        UpdateCodeDisplay();
        ShowMessage("ENTER THE ESCAPE CODE");
    }

    private void Delete_Clicked(object? sender, EventArgs e)
    {
        if (_finished || _busy || _current.Length == 0) return;

        _current = _current[..^1];
        UpdateCodeDisplay();
    }

    private void UpdateCodeDisplay()
    {
        CodeLabel.Text = string.Join(" ",
            Enumerable.Range(0, 3).Select(i => i < _current.Length ? _current[i].ToString() : "•"));
    }

    // ---------- Guessing ----------

    private async void Enter_Clicked(object? sender, EventArgs e)
    {
        if (_finished || _busy) return;

        if (_current.Length != 3 || !_engine.IsValidGuess(_current))
        {
            ShowMessage("ENTER ALL THREE DIGITS", Colors.OrangeRed);
            await ShakeView(CodeDisplay, 7);
            return;
        }

        var result = _engine.Evaluate(_current);
        _history.Insert(0, result);

        // Keep only as many past guesses as this level allows
        int limit = _engine.Settings.HistoryLimit;
        while (limit > 0 && _history.Count > limit)
            _history.RemoveAt(_history.Count - 1);

        GuessesLabel.Text = _engine.GuessCount.ToString();
        HitsLabel.Text = result.Hits.ToString();
        MatchesLabel.Text = result.Matches.ToString();

        if (result.Hits == 3)
            await EscapeSequence();
        else
            await AccessDeniedSequence();
    }

    private async Task AccessDeniedSequence()
    {
        _busy = true;

        SetTerminal(Colors.Red);
        SetStatus("🔒", "ACCESS DENIED", Colors.Red, "Incorrect code. The cell remains locked.");
        ShowMessage("ACCESS DENIED — TRY AGAIN", Colors.Red);

        await ShakeView(KeypadPanel, 14);
        await Task.Delay(300);

        if (!_finished)
        {
            _current = "";
            UpdateCodeDisplay();
            SetTerminal(null);
            SetStatus("🔒", "CELL LOCKED", Orange, "Keep cracking the code.");
            ShowMessage("ENTER THE ESCAPE CODE");
        }
        _busy = false;
    }

    // ---------- Help ----------

    private async void HowToPlay_Clicked(object? sender, EventArgs e)
    {
        _timer.Stop();               // pause the clock while reading
        await HowToPlay.ShowAsync();
    }

    private void OnHelpClosed(object? sender, EventArgs e)
    {
        if (!_finished) _timer.Start();
    }

    // ---------- Win: the full-screen escape ----------

    private async Task EscapeSequence()
    {
        _finished = true;
        _timer.Stop();

        SetTerminal(Colors.LimeGreen);
        EnterButton.IsEnabled = false;
        EnterButton.BackgroundColor = Color.FromArgb("#177A35");
        EnterButton.BorderColor = Colors.LimeGreen;
        SetStatus("🔓", "CELL UNLOCKED", Colors.LimeGreen, "Correct code! The cell door is opening...");
        ShowMessage("CORRECT CODE!", Colors.LimeGreen);

        await HitsPanel.ScaleTo(1.08, 150);
        await HitsPanel.ScaleTo(1, 150);
        await Task.Delay(500);

        // 1. Cut to the locked cell, full screen
        PrisonImage.Source = "prison_locked.png";
        PrisonImage.Opacity = 1;
        PrisonerImage.Source = RunnerImage;
        PrisonerImage.IsVisible = false;
        PrisonerImage.Opacity = 0;
        PrisonerImage.Rotation = 0;
        SceneCaption.Text = "CELL UNLOCKED";
        EscapeScene.Opacity = 0;
        EscapeScene.IsVisible = true;
        await EscapeScene.FadeTo(1, 500);
        await Task.Delay(700);

        // 2. The door swings open (crossfade to the open-door picture)
        SceneCaption.Text = "THE DOOR OPENS...";
        await PrisonImage.FadeTo(0.2, 250);
        PrisonImage.Source = "prison_escaped.png";
        await PrisonImage.FadeTo(1, 350);
        await Task.Delay(400);

        // 3. The prisoner appears inside the cell, to the left of the open door
        double w = EscapeScene.Width;
        double h = EscapeScene.Height;

        PrisonerImage.ScaleX = RunnerFacesRight ? 1 : -1;   // flips him if he faces the other way
        PrisonerImage.Scale = RunnerStartScale;
        PrisonerImage.TranslationX = -w * 0.22;             // left of centre
        PrisonerImage.TranslationY = h * 0.10;              // standing on the floor
        PrisonerImage.IsVisible = true;
        await PrisonerImage.FadeTo(1, 250);
        SceneCaption.Text = "RUN!";
        await Task.Delay(300);

        // 4. He runs across the cell to the open door, then out toward the camera
        bool running = true;
        Task wobble = RunWobbleAsync(() => running);

        await PrisonerImage.TranslateTo(w * 0.15, h * 0.10, 1300, Easing.SinInOut);   // to the doorway

        await Task.WhenAll(
            PrisonerImage.ScaleTo(RunnerStartScale * 3, 1400, Easing.CubicIn),
            PrisonerImage.TranslateTo(w * 0.05, h * 0.30, 1400, Easing.CubicIn));

        running = false;
        await wobble;

        // 5. He bursts past the camera into freedom
        await PrisonerImage.FadeTo(0, 300);
        PrisonerImage.IsVisible = false;
        SceneCaption.Text = "FREEDOM!";
        await Task.Delay(900);

        _score = _engine.CalculateScore(_secondsLeft);
        bool canSave = _leaderboard.QualifiesForTop20(_score);

        await ShowResult(
            "PRISONER ESCAPED!",
            Colors.LimeGreen,
            $"Attempts: {_engine.GuessCount}\nScore: {_score}",
            canSave);
    }

    // Rocks the prisoner left and right while keepGoing() is true, like a running figure
    private async Task RunWobbleAsync(Func<bool> keepGoing)
    {
        while (keepGoing())
        {
            await PrisonerImage.RotateTo(-7, 110);
            await PrisonerImage.RotateTo(7, 110);
        }
        await PrisonerImage.RotateTo(0, 80);
    }

    // ---------- Lose: out of time ----------

    private async Task TimeUpSequence()
    {
        _finished = true;
        _timer.Stop();

        SetTerminal(Colors.Red);
        EnterButton.IsEnabled = false;
        SetStatus("🚨", "BUSTED!", Colors.Red, "The guards caught you.");
        ShowMessage("ALARM! THE GUARDS ARE HERE!", Colors.Red);

        await ShakeView(KeypadPanel, 14);
        await ShowResult("BUSTED!", Colors.Red, "Time ran out. The guards caught you.");
    }

    // ---------- Leaderboard ----------

    private async void SaveScore_Clicked(object? sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        if (name.Length == 0)
        {
            SaveError.Text = "Please enter a name first.";
            SaveError.IsVisible = true;
            return;
        }

        _leaderboard.Add(new ScoreEntry
        {
            Username = name,
            Score = _score,
            Difficulty = _engine.Difficulty
        });
        Preferences.Set("lastName", name);

        SavePanel.IsVisible = false;
        await Shell.Current.GoToAsync("//LeaderboardPage");
    }

    private async void Leaderboard_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LeaderboardPage");
    }

    // ---------- New escape / back ----------

    // Asks before throwing away a game that is still in progress
    private async Task<bool> ConfirmLeaveAsync(string question)
    {
        if (_finished || _engine.GuessCount == 0) return true;

        _timer.Stop();
        bool yes = await DisplayAlert("Are you sure?", question, "Yes", "Keep playing");
        if (!yes && !_finished) _timer.Start();
        return yes;
    }

    private async void NewEscape_Clicked(object? sender, EventArgs e)
    {
        if (!await ConfirmLeaveAsync("Start a new escape? Your current attempt will be lost."))
            return;

        _timer.Stop();
        _engine.NewGame(_engine.Difficulty);   // new secret code, same level
        StartRound();
    }

    private async void BackToMenu_Clicked(object? sender, EventArgs e)
    {
        if (!await ConfirmLeaveAsync("Go back to the cell block? Your progress will be lost."))
            return;

        _timer.Stop();
        await Shell.Current.GoToAsync("//StartPage");
    }

    // ---------- Helpers ----------

    private void SetStatus(string icon, string title, Color color, string description)
    {
        StatusIcon.Text = icon;
        StatusTitle.Text = title;
        StatusTitle.TextColor = color;
        StatusDescription.Text = description;
    }

    // accent == null returns the terminal to its normal look
    private void SetTerminal(Color? accent)
    {
        CodeLabel.TextColor = accent ?? Orange;
        CodeDisplay.Stroke = accent ?? DisplayDark;
        KeypadPanel.Stroke = accent ?? Metal;
    }

    private void ShowMessage(string text, Color? color = null)
    {
        MessageLabel.Text = text;
        MessageLabel.TextColor = color ?? Soft;
        MessagePanel.Stroke = color ?? Slate;
    }

    private async Task ShowResult(string title, Color color, string detail, bool canSave = false)
    {
        ResultTitle.Text = title;
        ResultTitle.TextColor = color;
        ResultDetail.Text = detail;

        SavePanel.IsVisible = canSave;
        SaveError.IsVisible = false;
        if (canSave)
            NameEntry.Text = Preferences.Get("lastName", "");   // remembers the last name used

        ResultPanel.Opacity = 0;
        ResultPanel.IsVisible = true;
        await ResultPanel.FadeTo(1, 350);
    }

    private static async Task ShakeView(VisualElement view, double amount)
    {
        foreach (double x in new[] { -amount, amount, -amount * 0.7, amount * 0.7, -amount * 0.4, amount * 0.4, 0 })
            await view.TranslateTo(x, 0, 45);
    }
}