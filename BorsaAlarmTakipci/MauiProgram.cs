using Microsoft.Extensions.Logging;
using BorsaAlarmTakipci.Services;
using BorsaAlarmTakipci.ViewModels;
using BorsaAlarmTakipci.Views;
using Plugin.LocalNotification;

namespace BorsaAlarmTakipci;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification() // Yerel bildirim için
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Servisler
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<FinancialDataService>();
        builder.Services.AddSingleton<NotificationService>();

        // ViewModels
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddTransient<AddAlarmPageViewModel>();

        // Views
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<Views.AddAlarmPage>();



#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
