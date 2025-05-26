using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System;

namespace BorsaAlarmTakipci.Services
{
    /// <summary>
    /// Finansal veri API'sine erişim sağlayan servis sınıfı.
    /// Yahoo Finance API kullanarak hisse senedi fiyatlarını getirir.
    /// Önbellekleme ve yeniden deneme mekanizmaları içerir.
    /// </summary>
    public class FinancialDataService
    {
        private readonly HttpClient _httpClient;

        // Önbellek mekanizması için dictionary
        private readonly Dictionary<string, CachedPrice> _priceCache = new Dictionary<string, CachedPrice>();

        // Önbellek süresi: 15 dakika boyunca aynı hisse için tekrar istek göndermez
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

        // Yeniden deneme mekanizması için parametreler
        private readonly int _maxRetryAttempts = 3; // Maksimum 3 kez deneme yapılacak
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2); // Denemeler arası 2 saniye bekleme

        /// <summary>
        /// FinancialDataService yapıcı metodu.
        /// HttpClient oluşturur ve User-Agent header'ını ayarlar.
        /// </summary>
        public FinancialDataService()
        {
            _httpClient = new HttpClient();
            // User-Agent header'ı ekleyerek bazı API kısıtlamalarını aşmaya yardımcı olur
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64 ) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        /// <summary>
        /// Belirtilen hisse senedi sembolü için gerçek zamanlı fiyat bilgisini getirir.
        /// Önbellekleme mekanizması kullanır ve Türk hisseleri için sembol formatını düzeltir.
        /// </summary>
        /// <param name="stockSymbol">Hisse senedi sembolü (örn: KCHOL, ASELS)</param>
        /// <returns>Fiyat bilgisi içeren PriceData nesnesi veya null</returns>
        public async Task<PriceData> GetRealTimePriceAsync(string stockSymbol)
        {
            try
            {
                // Türk hisseleri için sembol formatını düzelt
                string symbolToUse = stockSymbol;
                if (!stockSymbol.Contains(".") && !stockSymbol.StartsWith("^"))
                {
                    // Türk hisseleri için Yahoo Finance formatı: KCHOL.IS
                    symbolToUse = stockSymbol + ".IS";
                }

                // Önbellekte bu sembol için güncel veri var mı kontrol et
                if (_priceCache.TryGetValue(symbolToUse, out var cachedPrice))
                {
                    if (DateTime.Now - cachedPrice.Timestamp < _cacheDuration)
                    {
                        Debug.WriteLine($"Önbellekten alınan fiyat ({symbolToUse}): {cachedPrice.Price}");
                        return new PriceData { Price = cachedPrice.Price.ToString(), ParsedPrice = cachedPrice.Price };
                    }
                }

                // Önbellekte güncel veri yoksa, API'den al (yeniden deneme mekanizması ile)
                return await GetPriceWithRetryAsync(symbolToUse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fiyat verisi alınırken hata: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Yeniden deneme mekanizması ile API'den fiyat bilgisini getirir.
        /// 429 (Too Many Requests) hatası alındığında belirli bir süre bekleyip tekrar dener.
        /// </summary>
        /// <param name="symbol">Hisse senedi sembolü (Yahoo Finance formatında)</param>
        /// <returns>Fiyat bilgisi içeren PriceData nesnesi veya null</returns>
        private async Task<PriceData> GetPriceWithRetryAsync(string symbol)
        {
            int attempts = 0;
            while (attempts < _maxRetryAttempts)
            {
                try
                {
                    attempts++;

                    // Yahoo Finance API endpoint'i
                    string requestUrl = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d";
                    Debug.WriteLine($"API İstek URL'si: {requestUrl} (Deneme: {attempts} )");

                    var response = await _httpClient.GetAsync(requestUrl);

                    // 429 (Too Many Requests) hatası alındığında yeniden dene
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (attempts < _maxRetryAttempts)
                        {
                            Debug.WriteLine($"429 Too Many Requests hatası alındı. {_retryDelay.TotalSeconds} saniye bekleyip yeniden deneniyor...");
                            await Task.Delay(_retryDelay);
                            continue;
                        }
                    }

                    response.EnsureSuccessStatusCode();

                    var jsonString = await response.Content.ReadAsStringAsync();

                    // Yahoo Finance yanıtını ayrıştır
                    var yahooResponse = JsonSerializer.Deserialize<YahooFinanceResponse>(jsonString);

                    if (yahooResponse?.Chart?.Result != null && yahooResponse.Chart.Result.Count > 0)
                    {
                        var result = yahooResponse.Chart.Result[0];
                        if (result.Meta?.RegularMarketPrice != null)
                        {
                            double price = result.Meta.RegularMarketPrice.Value;
                            Debug.WriteLine($"API Yanıtı ({symbol}): Fiyat = {price}");

                            // Önbelleğe ekle
                            _priceCache[symbol] = new CachedPrice { Price = price, Timestamp = DateTime.Now };

                            return new PriceData
                            {
                                Price = price.ToString(),
                                ParsedPrice = price
                            };
                        }
                    }

                    Debug.WriteLine($"{symbol} için fiyat alınamadı.");
                    return null;
                }
                catch (HttpRequestException ex) when (attempts < _maxRetryAttempts)
                {
                    Debug.WriteLine($"HTTP isteği başarısız oldu: {ex.Message}. Yeniden deneniyor ({attempts}/{_maxRetryAttempts})");
                    await Task.Delay(_retryDelay);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fiyat verisi alınırken beklenmeyen hata: {ex.Message}");
                    throw;
                }
            }

            Debug.WriteLine($"Maksimum deneme sayısına ulaşıldı ({_maxRetryAttempts}). İşlem başarısız.");
            return null;
        }

        /// <summary>
        /// Önbellek için kullanılan yardımcı sınıf.
        /// Fiyat ve zaman damgası bilgilerini tutar.
        /// </summary>
        private class CachedPrice
        {
            public double Price { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// Yahoo Finance API yanıt modeli - Ana sınıf
    /// </summary>
    public class YahooFinanceResponse
    {
        [JsonPropertyName("chart")]
        public ChartData Chart { get; set; }
    }

    /// <summary>
    /// Yahoo Finance API yanıt modeli - Chart verisi
    /// </summary>
    public class ChartData
    {
        [JsonPropertyName("result")]
        public List<ResultData> Result { get; set; }

        [JsonPropertyName("error")]
        public object Error { get; set; }
    }

    /// <summary>
    /// Yahoo Finance API yanıt modeli - Sonuç verisi
    /// </summary>
    public class ResultData
    {
        [JsonPropertyName("meta")]
        public MetaData Meta { get; set; }
    }

    /// <summary>
    /// Yahoo Finance API yanıt modeli - Meta verisi
    /// </summary>
    public class MetaData
    {
        [JsonPropertyName("regularMarketPrice")]
        public double? RegularMarketPrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    /// <summary>
    /// Fiyat verisi sınıfı - Uygulama içinde kullanılır
    /// </summary>
    public class PriceData
    {
        [JsonPropertyName("price")]
        public string Price { get; set; }

        public double? ParsedPrice { get; set; }
    }
}
