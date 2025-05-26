using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Configuration;

namespace BorsaAlarmTakipci.Services
{
    public class FinancialDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey; // Artık kullanılmayacak, Yahoo Finance için API anahtarı gerekmiyor

        public FinancialDataService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["TwelveDataApiKey"]; // Artık kullanılmayacak
        }

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

                // Yahoo Finance API endpoint'i
                string requestUrl = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbolToUse}?interval=1d";
                Debug.WriteLine($"API İstek URL'si: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API Yanıtı (ham): {jsonString.Substring(0, Math.Min(jsonString.Length, 200))}...");

                // Yahoo Finance yanıtını ayrıştır
                var yahooResponse = JsonSerializer.Deserialize<YahooFinanceResponse>(jsonString);

                if (yahooResponse?.Chart?.Result != null && yahooResponse.Chart.Result.Count > 0)
                {
                    var result = yahooResponse.Chart.Result[0];
                    if (result.Meta?.RegularMarketPrice != null)
                    {
                        double price = result.Meta.RegularMarketPrice.Value;
                        Debug.WriteLine($"API Yanıtı ({stockSymbol}): Fiyat = {price}");

                        return new PriceData
                        {
                            Price = price.ToString(),
                            ParsedPrice = price
                        };
                    }
                }

                Debug.WriteLine($"{stockSymbol} için fiyat alınamadı.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fiyat verisi alınırken hata: {ex.Message}");
                return null;
            }
        }
    }

    // Yahoo Finance API yanıt modelleri
    public class YahooFinanceResponse
    {
        [JsonPropertyName("chart")]
        public ChartData Chart { get; set; }
    }

    public class ChartData
    {
        [JsonPropertyName("result")]
        public List<ResultData> Result { get; set; }

        [JsonPropertyName("error")]
        public object Error { get; set; }
    }

    public class ResultData
    {
        [JsonPropertyName("meta")]
        public MetaData Meta { get; set; }
    }

    public class MetaData
    {
        [JsonPropertyName("regularMarketPrice")]
        public double? RegularMarketPrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    // Mevcut PriceData sınıfını koruyoruz, böylece diğer kodları değiştirmemize gerek kalmaz
    public class PriceData
    {
        [JsonPropertyName("price")]
        public string Price { get; set; }

        // Bu özellik artık doğrudan Yahoo Finance'den gelen değeri kullanacak
        public double? ParsedPrice { get; set; }
    }
}
