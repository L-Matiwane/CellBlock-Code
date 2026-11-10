using CellBlock_Code.Services;
using CellBlock_Code.Views;

namespace CellBlock_Code
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            //#if DEBUG
            //builder.Logging.AddDebug();
            //#endif

            builder.Services.AddSingleton<GameEngine>();
            builder.Services.AddSingleton<LeaderboardService>();
            builder.Services.AddTransient<StartPage>();
            builder.Services.AddTransient<GamePage>();
            builder.Services.AddTransient<LeaderboardPage>();

            return builder.Build();
        }
    }
}
