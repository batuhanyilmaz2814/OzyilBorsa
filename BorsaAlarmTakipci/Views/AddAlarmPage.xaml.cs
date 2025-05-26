using BorsaAlarmTakipci.Models;
using BorsaAlarmTakipci.Services;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

namespace BorsaAlarmTakipci.Views
{
    public partial class AddAlarmPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public AddAlarmPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                string stockSymbol = StockSymbolEntry.Text;
                string upperLimitText = UpperLimitEntry.Text;
                string lowerLimitText = LowerLimitEntry.Text;

                // Validasyon
                if (string.IsNullOrWhiteSpace(stockSymbol))
                {
                    await DisplayAlert("Hata", "Hisse sembol� bo� olamaz.", "Tamam");
                    return;
                }

                // En az bir limit belirtilmeli
                if (string.IsNullOrWhiteSpace(upperLimitText) && string.IsNullOrWhiteSpace(lowerLimitText))
                {
                    await DisplayAlert("Hata", "En az bir fiyat limiti belirtmelisiniz.", "Tamam");
                    return;
                }

                // Limitleri parse et
                double? upperLimitValue = null;
                double? lowerLimitValue = null;

                if (!string.IsNullOrWhiteSpace(upperLimitText))
                {
                    if (double.TryParse(upperLimitText, out double parsedUpperVal))
                    {
                        upperLimitValue = parsedUpperVal;
                    }
                    else
                    {
                        await DisplayAlert("Hata", "�st limit ge�erli bir say� de�il.", "Tamam");
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(lowerLimitText))
                {
                    if (double.TryParse(lowerLimitText, out double parsedLowerVal))
                    {
                        lowerLimitValue = parsedLowerVal;
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Alt limit ge�erli bir say� de�il.", "Tamam");
                        return;
                    }
                }

                // Limitleri kar��la�t�r
                if (upperLimitValue.HasValue && lowerLimitValue.HasValue && upperLimitValue <= lowerLimitValue)
                {
                    await DisplayAlert("Hata", "�st limit, alt limitten b�y�k olmal�d�r.", "Tamam");
                    return;
                }

                // Alarm olu�tur ve kaydet
                var alarm = new AlarmDefinition
                {
                    StockSymbol = stockSymbol.ToUpper().Trim(),
                    UpperLimit = upperLimitValue,
                    LowerLimit = lowerLimitValue,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _databaseService.SaveAlarmAsync(alarm);
                await DisplayAlert("Ba�ar�l�", "Alarm ba�ar�yla kaydedildi.", "Tamam");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Alarm kaydedilirken hata: {ex.Message}");
                await DisplayAlert("Hata", $"Alarm kaydedilirken bir sorun olu�tu: {ex.Message}", "Tamam");
            }
        }

        private async void OnCancelButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
