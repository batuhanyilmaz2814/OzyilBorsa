using Microsoft.Maui.Controls;
using Plugin.LocalNotification;

namespace BorsaAlarmTakipci;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();

        // Bildirim izinlerini iste
        RequestNotificationPermission();
    }

    private async void RequestNotificationPermission()
    {
        var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
        System.Diagnostics.Debug.WriteLine($"Bildirim izni sonucu: {result}");
    }
}
