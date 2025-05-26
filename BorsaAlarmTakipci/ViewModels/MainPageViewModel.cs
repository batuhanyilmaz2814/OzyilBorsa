using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BorsaAlarmTakipci.Models;  // AlarmDefinition modelimiz
using BorsaAlarmTakipci.Services; // DatabaseService'imiz
//using BorsaAlarmTakipci.Views;   // AddAlarmPage'e gitmek için (henüz oluşturmadık)
using Microsoft.Maui.Controls; // Shell navigasyonu için
using Microsoft.Maui.ApplicationModel; // MainThread için
using System.Diagnostics; // Debug sınıfı için bu satırı ekleyin
using System.Timers; // Timer sınıfı için bu satırı ekleyin


namespace BorsaAlarmTakipci.ViewModels
{
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly FinancialDataService _financialDataService; // EKLENDİ
        private System.Timers.Timer _priceCheckTimer;
        // Timer için bir alan ekleyelim
        private const double PriceCheckIntervalMinutes = 0.5; //Kontrol aralığı (dakika cinsinden)

        [ObservableProperty]
        private ObservableCollection<AlarmDefinition> _alarms;

        [ObservableProperty]
        private bool _isBusy; // Yükleme durumu için

        public MainPageViewModel(DatabaseService databaseService, FinancialDataService financialDataService)
        {
            _databaseService = databaseService;
            _financialDataService = financialDataService;
            _alarms = new ObservableCollection<AlarmDefinition>();
            InitializeTimer();
            
            // Sayfa ilk yüklendiğinde alarmları getir
            // LoadAlarmsCommand.Execute(null); // Bu şekilde de çağrılabilir veya doğrudan metot çağrılır.
            // ViewModel oluşturulduğunda alarmları yüklemek için bir metot çağırabiliriz.
            // Ancak, sayfa göründüğünde yüklemek daha iyi bir pratik olabilir (OnAppearing).
        }

        private void InitializeTimer()
        {
            _priceCheckTimer = new System.Timers.Timer();
            _priceCheckTimer.Interval = TimeSpan.FromMinutes(PriceCheckIntervalMinutes).TotalMilliseconds;
            _priceCheckTimer.Elapsed += async (sender, e) => await OnTimerElapsed();
            _priceCheckTimer.AutoReset = true; // Her tetiklendiğinde yeniden başlasın
            // Timer'ı sayfa göründüğünde başlatacağız.
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

                            // UI thread'inde bir uyarı gösterelim
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await Shell.Current.DisplayAlert("Alarm Tetiklendi!",
                                    $"{alarm.StockSymbol} hissesinin fiyatı ({currentPrice}), belirlediğiniz üst limit olan {alarm.UpperLimit.Value} değerine ulaştı veya geçti!",
                                    "Tamam");
                            });
                        }
                        else if (lowerLimitTriggered)
                        {
                            Debug.WriteLine($"ALARM TETİKLENDİ (ALT LİMİT): {alarm.StockSymbol} fiyatı {currentPrice}, alt limit olan {alarm.LowerLimit.Value} değerine ulaştı veya altına düştü!");

                            // UI thread'inde bir uyarı gösterelim
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await Shell.Current.DisplayAlert("Alarm Tetiklendi!",
                                    $"{alarm.StockSymbol} hissesinin fiyatı ({currentPrice}), belirlediğiniz alt limit olan {alarm.LowerLimit.Value} değerine ulaştı veya altına düştü!",
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


        // Sayfa göründüğünde zamanlayıcıyı başlatmak ve alarmları yüklemek için metot
        // Bu metot, MainPage.xaml.cs içindeki OnAppearing() metodundan çağrılacak.
        public async Task OnPageAppearingAsync()
        {
            await LoadAlarmsAsync(); // Mevcut alarmları yükle
            if (!_priceCheckTimer.Enabled)
            {
                _priceCheckTimer.Start();
                Debug.WriteLine("Fiyat kontrol zamanlayıcısı başlatıldı.");
            }
        }

        // Sayfa kaybolduğunda zamanlayıcıyı durdurmak için metot
        // Bu metot, MainPage.xaml.cs içindeki OnDisappearing() metodundan çağrılacak.
        public void OnPageDisappearing()
        {
            if (_priceCheckTimer.Enabled)
            {
                _priceCheckTimer.Stop();
                Debug.WriteLine("Fiyat kontrol zamanlayıcısı durduruldu.");
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
            catch (System.Exception ex)
            {
                // Hata yönetimi: Kullanıcıya bir mesaj gösterilebilir
                await Shell.Current.DisplayAlert("Hata", $"Alarmlar yüklenirken bir sorun oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // IDisposable implementasyonu
        public void Dispose()
        {
            _priceCheckTimer?.Stop();
            _priceCheckTimer?.Dispose();
            GC.SuppressFinalize(this);
        }


        [RelayCommand]
        private async Task GoToAddAlarmPageAsync()
        {
            // AddAlarmPage henüz oluşturulmadı, oluşturulduktan sonra bu satır çalışacaktır.
            // await Shell.Current.GoToAsync(nameof(AddAlarmPage));
            // Şimdilik geçici bir uyarı gösterelim:
            //await Shell.Current.DisplayAlert("Uyarı", "Alarm ekleme sayfası henüz oluşturulmadı.", "Tamam");
            await Shell.Current.GoToAsync("AddAlarmPage"); // Rota olarak tanımlanacak
        }

        // Bir alarmı silmek için komut (SwipeView ile kullanılabilir)
        [RelayCommand]
        private async Task DeleteAlarmAsync(AlarmDefinition alarmToDelete)
        {
            if (alarmToDelete == null) return;

            // Kullanıcıdan onay al
            bool confirmed = await Shell.Current.DisplayAlert(
                "Alarmı Sil",
                $"'{alarmToDelete.StockSymbol}' sembollü alarmı silmek istediğinizden emin misiniz?",
                "Evet",
                "Hayır");

            if (confirmed)
            {
                try
                {
                    await _databaseService.DeleteAlarmAsync(alarmToDelete);
                    Alarms.Remove(alarmToDelete); // Koleksiyondan da kaldır
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Alarm silinirken hata: {ex.Message}");
                    await Shell.Current.DisplayAlert("Hata", "Alarm silinirken bir sorun oluştu.", "Tamam");
                }
            }
        }

        // ... (MainPageViewModel.cs içinde)
        [RelayCommand]
        private async Task TestApiAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Test için bir hisse senedi sembolü
                string testSymbol = "THYAO"; // Veya başka bir geçerli sembol
                var priceData = await _financialDataService.GetRealTimePriceAsync(testSymbol);
                if (priceData != null && priceData.ParsedPrice.HasValue)
                {
                    Debug.WriteLine($"TEST API: {testSymbol} fiyatı: {priceData.ParsedPrice.Value}");
                    await Shell.Current.DisplayAlert("API Test Sonucu", $"{testSymbol} için anlık fiyat: {priceData.ParsedPrice.Value}", "Tamam");
                }
                else if (priceData != null && priceData.Price != null)
                {
                    Debug.WriteLine($"TEST API: {testSymbol} fiyatı (string): {priceData.Price}");
                    await Shell.Current.DisplayAlert("API Test Sonucu", $"{testSymbol} için anlık fiyat (string): {priceData.Price}", "Tamam");
                }
                else
                {
                    Debug.WriteLine($"TEST API: {testSymbol} için fiyat alınamadı.");
                    await Shell.Current.DisplayAlert("API Test Sonucu", $"{testSymbol} için fiyat verisi alınamadı.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TEST API Hatası: {ex.Message}");
                await Shell.Current.DisplayAlert("API Test Hatası", $"Hata: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

    }

}
