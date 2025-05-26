using Plugin.LocalNotification;
using System;
using System.Threading.Tasks;
using BorsaAlarmTakipci.Models;

namespace BorsaAlarmTakipci.Services
{
    /// <summary>
    /// Yerel bildirim göndermek için kullanılan servis sınıfı.
    /// </summary>
    public class NotificationService
    {
        /// <summary>
        /// Fiyat alarmı tetiklendiğinde bildirim gönderir.
        /// </summary>
        /// <param name="alarm">Tetiklenen alarm</param>
        /// <param name="currentPrice">Mevcut fiyat</param>
        /// <param name="isUpperLimit">Üst limit mi tetiklendi?</param>
        /// <returns>Task</returns>
        public async Task SendPriceAlertNotificationAsync(AlarmDefinition alarm, double currentPrice, bool isUpperLimit)
        {
            try
            {
                string title = $"Fiyat Alarmı: {alarm.StockSymbol}";
                string message = isUpperLimit
                    ? $"{alarm.StockSymbol} fiyatı ({currentPrice:N2} ₺), belirlediğiniz üst limit olan {alarm.UpperLimit:N2} ₺ değerine ulaştı veya geçti!"
                    : $"{alarm.StockSymbol} fiyatı ({currentPrice:N2} ₺), belirlediğiniz alt limit olan {alarm.LowerLimit:N2} ₺ değerine ulaştı veya altına düştü!";

                var request = new NotificationRequest
                {
                    NotificationId = alarm.Id, // Her alarm için benzersiz bir ID
                    Title = title,
                    Description = message,
                    ReturningData = $"alarm_{alarm.Id}", // Bildirime tıklandığında kullanılabilecek veri
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now // Hemen bildirim gönder
                    }
                };

                await LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bildirim gönderilirken hata: {ex.Message}");
            }
        }
    }
}
