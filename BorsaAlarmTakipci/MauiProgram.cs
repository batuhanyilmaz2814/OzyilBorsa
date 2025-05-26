using Microsoft.Extensions.Logging;
using BorsaAlarmTakipci.Services;
using BorsaAlarmTakipci.ViewModels;
using BorsaAlarmTakipci.Views;
using Microsoft.Extensions.Configuration; // IConfiguration için
using System.Reflection; // Assembly.GetExecutingAssembly() için


namespace BorsaAlarmTakipci
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

            //User Secrets
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("BorsaAlarmTakipci.appsettings.json"); // Eğer appsettings.json varsa

            var config = new ConfigurationBuilder()
                // .AddJsonStream(stream) // Eğer appsettings.json kullanıyorsanız
                .AddUserSecrets<App>() // User secrets'ı App sınıfının assembly'sinden yükle
                .Build();
            builder.Configuration.AddConfiguration(config); // Oluşturulan konfigürasyonu build

            //Database kaydı:

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<FinancialDataService>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<AddAlarmPageViewModel>(); // Transient: Her istendiğinde yeni bir örnek oluşturulur.
            builder.Services.AddTransient<AddAlarmPage>(); // AddAlarmPage için de benzer şekilde ekliyoruz.
            // Eğer ViewModel'larınız varsa ve onları da DI ile yönetmek isterseniz:
            // builder.Services.AddSingleton<MainPageViewModel>();
            // builder.Services.AddTransient<AddAlarmPageViewModel>(); // Transient: Her istendiğinde yeni bir örnek oluşturulur.


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
