namespace CellBlock_Code.Views;

public partial class HowToPlayView : ContentView
{
    public event EventHandler? Closed;

    public HowToPlayView()
    {
        InitializeComponent();
    }

    public async Task ShowAsync()
    {
        Opacity = 0;
        IsVisible = true;
        await this.FadeTo(1, 250);
    }

    private async void Close_Clicked(object? sender, EventArgs e)
    {
        await this.FadeTo(0, 200);
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}