using BorsaAlarmTakipci.Models;
using BorsaAlarmTakipci.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BorsaAlarmTakipci.ViewModels
{
    public partial class AddAlarmPageViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private string _stockSymbol;

        [ObservableProperty]
        private string _upperLimit;

        [ObservableProperty]
        private string _lowerLimit;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isBusy;

        public AddAlarmPageViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [RelayCommand]
        private async Task SaveAlarmAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Validasyon
                if (string.IsNullOrWhiteSpace(StockSymbol))
                {
                    await Shell.Current.DisplayAlert("Hata", "Hisse sembolü boş olamaz.", "Tamam");
                    return;
                }

                // En az bir limit belirtilmeli
                if (string.IsNullOrWhiteSpace(UpperLimit) && string.IsNullOrWhiteSpace(LowerLimit))
                {
                    await Shell.Current.DisplayAlert("Hata", "En az bir fiyat limiti belirtmelisiniz.", "Tamam");
                    return;
                }

                // Limitleri parse et
                double? upperLimitValue = null;
                double? lowerLimitValue = null;

                if (!string.IsNullOrWhiteSpace(UpperLimit))
                {
                    if (double.TryParse(UpperLimit, out double parsedUpperVal))
                    {
                        upperLimitValue = parsedUpperVal;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Hata", "Üst limit geçerli bir sayı değil.", "Tamam");
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(LowerLimit))
                {
                    if (double.TryParse(LowerLimit, out double parsedLowerVal))
                    {
                        lowerLimitValue = parsedLowerVal;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Hata", "Alt limit geçerli bir sayı değil.", "Tamam");
                        return;
                    }
                }

                // Limitleri karşılaştır
                if (upperLimitValue.HasValue && lowerLimitValue.HasValue && upperLimitValue <= lowerLimitValue)
                {
                    await Shell.Current.DisplayAlert("Hata", "Üst limit, alt limitten büyük olmalıdır.", "Tamam");
                    return;
                }

                // Alarm oluştur ve kaydet
                var alarm = new AlarmDefinition
                {
                    StockSymbol = StockSymbol.ToUpper().Trim(),
                    UpperLimit = upperLimitValue,
                    LowerLimit = lowerLimitValue,
                    IsActive = IsActive,
                    CreatedAt = DateTime.Now
                };

                await _databaseService.SaveAlarmAsync(alarm);
                await Shell.Current.DisplayAlert("Başarılı", "Alarm başarıyla kaydedildi.", "Tamam");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Alarm kaydedilirken hata: {ex.Message}");
                await Shell.Current.DisplayAlert("Hata", $"Alarm kaydedilirken bir sorun oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
