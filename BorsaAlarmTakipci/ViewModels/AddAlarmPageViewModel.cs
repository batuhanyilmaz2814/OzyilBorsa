using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using BorsaAlarmTakipci.Models;
using BorsaAlarmTakipci.Services;
using Microsoft.Maui.Controls; // Shell navigasyonu ve DisplayAlert için
using System.Linq; // String validasyonu için

namespace BorsaAlarmTakipci.ViewModels
{
    // QueryProperty attribute'ü, navigasyon sırasında parametre almak için kullanılır (düzenleme senaryosu için)
    // [QueryProperty(nameof(AlarmToEditId), "alarmId")] 
    public partial class AddAlarmPageViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        // private int _alarmToEditId; // Düzenlenecek alarmın ID'si
        // private AlarmDefinition _alarmToEdit; // Düzenlenecek alarm nesnesi

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAlarmCommand))]
        private string _stockSymbol;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAlarmCommand))] // EKLENDİ
        private double? _upperLimit;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAlarmCommand))] // EKLENDİ
        private double? _lowerLimit;

        [ObservableProperty]
        private string _pageTitle = "Yeni Alarm Ekle";

        public AddAlarmPageViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // Düzenleme için ID geldiğinde alarmı yüklemek için özellik
        /*
        public int AlarmToEditId
        {
            get => _alarmToEditId;
            set
            {
                _alarmToEditId = value;
                if (_alarmToEditId != 0)
                {
                    LoadAlarmToEditAsync(_alarmToEditId);
                }
            }
        }

        private async Task LoadAlarmToEditAsync(int alarmId)
        {
            _alarmToEdit = await _databaseService.GetAlarmAsync(alarmId);
            if (_alarmToEdit != null)
            {
                StockSymbol = _alarmToEdit.StockSymbol;
                UpperLimit = _alarmToEdit.UpperLimit;
                LowerLimit = _alarmToEdit.LowerLimit;
                PageTitle = "Alarmı Düzenle";
            }
        }
        */

        private bool CanSaveAlarm()
        {
            // Hisse sembolü boş olmamalı ve en az bir limit girilmeli
            return !string.IsNullOrWhiteSpace(StockSymbol) && (UpperLimit.HasValue || LowerLimit.HasValue);
        }

        [RelayCommand(CanExecute = nameof(CanSaveAlarm))]
        private async Task SaveAlarmAsync()
        {
            if (!CanSaveAlarm())
            {
                await Shell.Current.DisplayAlert("Hata", "Lütfen hisse sembolünü ve en az bir fiyat limitini girin.", "Tamam");
                return;
            }

            // Sembolün büyük harf olduğundan emin olalım (isteğe bağlı)
            StockSymbol = StockSymbol.ToUpperInvariant();

            AlarmDefinition alarm = new AlarmDefinition // _alarmToEdit ?? new AlarmDefinition(); // Düzenleme varsa onu kullan, yoksa yeni oluştur
            {
                // Id = _alarmToEdit?.Id ?? 0, // Düzenleme varsa ID'sini koru
                StockSymbol = this.StockSymbol,
                UpperLimit = this.UpperLimit,
                LowerLimit = this.LowerLimit,
                IsEnabled = true // Yeni eklenen alarm varsayılan olarak aktif olsun
            };

            // Eğer düzenleme yapılıyorsa ve _alarmToEdit null değilse, CreatedDate'i koru
            // if (_alarmToEdit != null) alarm.CreatedDate = _alarmToEdit.CreatedDate;

            await _databaseService.SaveAlarmAsync(alarm);

            // Kaydettikten sonra ana sayfaya geri dön
            // MainPageViewModel'deki LoadAlarmsAsync'ın tekrar çağrılması gerekebilir.
            // Bu, Shell navigasyonunda ".." ile geri gidildiğinde OnAppearing ile tetiklenebilir.
            await Shell.Current.GoToAsync(".."); // Bir önceki sayfaya git
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync(".."); // Bir önceki sayfaya git
        }
    }
}
