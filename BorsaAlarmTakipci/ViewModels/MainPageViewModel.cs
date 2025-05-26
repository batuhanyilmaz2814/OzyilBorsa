using BorsaAlarmTakipci.Models;
using BorsaAlarmTakipci.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace BorsaAlarmTakipci.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly FinancialDataService _financialDataService;
        private readonly NotificationService _notificationService; // Eklendi
        private System.Timers.Timer _timer;
        private const double PriceCheckIntervalMinutes = 0.5; // 5 dakika

        [ObservableProperty]
        private ObservableCollection<AlarmDefinition> _alarms;

        [ObservableProperty]
        private bool _isBusy;

        public MainPageViewModel(DatabaseService databaseService, FinancialDataService financialDataService, NotificationService notificationService) // Güncellendi
        {
            _databaseService = databaseService;
            _financialDataService = financialDataService;
            _notificationService = notificationService; // Eklendi
            _alarms = new ObservableCollection<AlarmDefinition>();
        }

        // Sayfa göründüğünde çağrılır
        public async Task OnPageAppearingAsync()
        {
            await LoadAlarmsAsync();
            StartPriceCheckTimer();
        }

        // Sayfa kaybolduğunda çağrılır
        public void OnPageDisappearing()
        {
            StopPriceCheckTimer();
        }

        // Zamanlayıcıyı başlatan metot
        private void StartPriceCheckTimer()
        {
            if (_timer == null)
            {
                _timer = new System.Timers.Timer(PriceCheckIntervalMinutes * 60 * 1000); // Dakikayı milisaniyeye çevir
                _timer.Elapsed += async (s, e) => await OnTimerElapsed();
                _timer.AutoReset = true;
            }

            if (!_timer.Enabled)
            {
                _timer.Start();
                Debug.WriteLine("Fiyat kontrol zamanlayıcısı başlatıldı.");
            }
        }

        // Zamanlayıcıyı durduran metot
        private void StopPriceCheckTimer()
        {
            if (_timer != null && _timer.Enabled)
            {
                _timer.Stop();
                Debug.WriteLine("Fiyat kontrol zamanlayıcısı durduruldu.");
            }
        }

        // Zamanlayıcı tetiklendiğinde çalışacak metot
        private async Task OnTimerElapsed()
        {
            Debug.WriteLine("Fiyat kontrol zamanlayıcısı tetiklendi.");
            if (IsBusy)
            {
                Debug.WriteLine("Fiyat kontrolü atlandı, başka bir işlem devam ediyor.");
                return;
            }

            IsBusy = true;
            try
            {
                var activeAlarms = await _databaseService.GetActiveAlarmsAsync();
                if (activeAlarms == null || !activeAlarms.Any())
                {
                    Debug.WriteLine("Aktif alarm bulunamadı.");
                    return;
                }

                Debug.WriteLine($"{activeAlarms.Count} adet aktif alarm için fiyat kontrolü başlıyor...");

                foreach (var alarm in activeAlarms)
                {
                    Debug.WriteLine($"Alarm kontrol ediliyor: {alarm.StockSymbol}, ÜstLimit: {alarm.UpperLimit}, AltLimit: {alarm.LowerLimit}");

                    var priceData = await _financialDataService.GetRealTimePriceAsync(alarm.StockSymbol);
                    if (priceData?.ParsedPrice != null)
                    {
                        double currentPrice = priceData.ParsedPrice.Value;
                        Debug.WriteLine($"Sembol: {alarm.StockSymbol}, Anlık Fiyat: {currentPrice}, Üst Limit: {alarm.UpperLimit}, Alt Limit: {alarm.LowerLimit}");

                        // Eşik kontrolü için ayrı boolean değişkenler kullanarak mantığı daha açık hale getiriyoruz
                        bool upperLimitTriggered = alarm.UpperLimit.HasValue && currentPrice >= alarm.UpperLimit.Value;
                        bool lowerLimitTriggered = alarm.LowerLimit.HasValue && currentPrice <= alarm.LowerLimit.Value;

                        Debug.WriteLine($"Üst limit tetiklendi mi: {upperLimitTriggered}, Alt limit tetiklendi mi: {lowerLimitTriggered}");

                        // Eşik kontrolü
                        if (upperLimitTriggered)
                        {
                            Debug.WriteLine($"ALARM TETİKLENDİ (ÜST LİMİT): {alarm.StockSymbol} fiyatı {currentPrice}, üst limit olan {alarm.UpperLimit.Value} değerine ulaştı veya geçti!");

                            // Yerel bildirim gönder
                            await _notificationService.SendPriceAlertNotificationAsync(alarm, currentPrice, true);

                            // UI thread'inde bir uyarı gösterelim (uygulama açıkken)
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await Shell.Current.DisplayAlert("Alarm Tetiklendi!",
                                    $"{alarm.StockSymbol} hissesinin fiyatı ({currentPrice:N2} ₺), belirlediğiniz üst limit olan {alarm.UpperLimit:N2} ₺ değerine ulaştı veya geçti!",
                                    "Tamam");
                            });
                        }
                        else if (lowerLimitTriggered)
                        {
                            Debug.WriteLine($"ALARM TETİKLENDİ (ALT LİMİT): {alarm.StockSymbol} fiyatı {currentPrice}, alt limit olan {alarm.LowerLimit.Value} değerine ulaştı veya altına düştü!");

                            // Yerel bildirim gönder
                            await _notificationService.SendPriceAlertNotificationAsync(alarm, currentPrice, false);

                            // UI thread'inde bir uyarı gösterelim (uygulama açıkken)
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await Shell.Current.DisplayAlert("Alarm Tetiklendi!",
                                    $"{alarm.StockSymbol} hissesinin fiyatı ({currentPrice:N2} ₺), belirlediğiniz alt limit olan {alarm.LowerLimit:N2} ₺ değerine ulaştı veya altına düştü!",
                                    "Tamam");
                            });
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{alarm.StockSymbol} için fiyat alınamadı.");
                    }
                    await Task.Delay(1000); // API'ye çok sık istek atmamak için kısa bir bekleme
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fiyat kontrolü sırasında hata: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadAlarmsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Alarms.Clear();
                var alarmList = await _databaseService.GetAlarmsAsync();
                foreach (var alarm in alarmList)
                {
                    Alarms.Add(alarm);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Alarmlar yüklenirken hata: {ex.Message}");
                await Shell.Current.DisplayAlert("Hata", $"Alarmlar yüklenirken bir sorun oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToAddAlarmPageAsync()
        {
            await Shell.Current.GoToAsync("//AddAlarmPage");
        }

        [RelayCommand]
        private async Task DeleteAlarmAsync(AlarmDefinition alarmToDelete)
        {
            if (alarmToDelete == null) return;

            bool confirmed = await Shell.Current.DisplayAlert("Alarmı Sil", $"{alarmToDelete.StockSymbol} sembollü alarmı silmek istediğinizden emin misiniz?", "Evet", "Hayır");
            if (confirmed)
            {
                await _databaseService.DeleteAlarmAsync(alarmToDelete);
                Alarms.Remove(alarmToDelete);
            }
        }

        [RelayCommand]
        private async Task TestApiAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                string testSymbol = "THYAO"; // Test için THYAO kullanıyoruz
                var priceData = await _financialDataService.GetRealTimePriceAsync(testSymbol);

                if (priceData?.ParsedPrice != null)
                {
                    await Shell.Current.DisplayAlert("API Test Başarılı",
                        $"{testSymbol} hissesinin anlık fiyatı: {priceData.ParsedPrice.Value:N2} ₺",
                        "Tamam");
                }
                else
                {
                    await Shell.Current.DisplayAlert("API Test Başarısız",
                        $"{testSymbol} için fiyat alınamadı.",
                        "Tamam");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("API Test Hatası",
                    $"Test sırasında bir hata oluştu: {ex.Message}",
                    "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
