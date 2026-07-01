using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Skyguard.Grpc; // Kendi Proto Namespace'in

namespace Skyguard.Services
{
    public class LiveTelemetryService : global::Skyguard.Grpc.LiveTelemetryFeed.LiveTelemetryFeedBase
    {
        private readonly HttpClient _httpClient;

        // Bounding Box: Sadece Türkiye hava sahasındaki uçakları çeker
        private readonly string _openSkyUrl = "https://opensky-network.org/api/states/all?lamin=35.8&lomin=25.6&lamax=42.1&lomax=44.8";

        // Python FastAPI sunucumuzun adresi (Port 8081)
        private readonly string _pythonMlUrl = "http://127.0.0.1:8081/predict";

        public LiveTelemetryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // OpenSky'ın API'yi bloklamaması için User-Agent şarttır
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "SkyGuard_Backend_Node/1.0");
            }
        }

        // Proto'da tanımlı olan gRPC stream metodu
        public override async Task TelemetryData(TelemetryRequest request, IServerStreamWriter<TelemetryUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"[C2 TELEMETRY SERVER] Secure streaming started for target: {(string.IsNullOrEmpty(request.Flightno) ? "ALL_SYSTEM" : request.Flightno)}");

            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 1. ADIM: OpenSky'dan ham veriyi çek
                        var openSkyResponse = await _httpClient.GetAsync(_openSkyUrl, context.CancellationToken);

                        if (openSkyResponse.IsSuccessStatusCode)
                        {
                            var jsonResult = await openSkyResponse.Content.ReadAsStringAsync();
                            var flights = ParseOpenSkyJson(jsonResult);

                            Console.WriteLine($"[INFO] Fetched {flights.Count} targets from OpenSky. Forwarding to ML Engine...");

                            foreach (var flight in flights)
                            {
                                if (context.CancellationToken.IsCancellationRequested) break;

                                // 2. ADIM: Her bir uçağı Python (FastAPI) motoruna gönder, Risk Skorunu al
                                float riskScore = await GetRiskScoreFromPythonAsync(flight);

                                // 3. ADIM: Zenginleştirilmiş veriyi gRPC paketine sar ve WPF'e fırlat
                                var updatePacket = new TelemetryUpdate
                                {
                                    Flightno = flight.FlightNo,
                                    Callsign = flight.FlightNo, // OpenSky'da genellikle callsign olarak flightno çekiyoruz
                                    Velocity = (float)flight.Velocity,
                                    Heading = (float)flight.Heading,
                                    Baroaltitude = (float)flight.BaroAltitude,
                                    Latitude = flight.Latitude,
                                    Longitude = flight.Longitude,
                                    RiskScore = riskScore,
                                    LastUpdateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                };

                                await responseStream.WriteAsync(updatePacket);

                                // WPF UI thread'ini boğmamak için çok ufak bir bekleme (50ms)
                                await Task.Delay(50, context.CancellationToken);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[WARNING] OpenSky API returned HTTP {openSkyResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Pipeline exception during fetch/predict: {ex.Message}");
                    }

                    // 4. ADIM: OpenSky API kullanım sınırlarına takılmamak (ban yememek) için 10 saniye bekle
                    Console.WriteLine("[SYSTEM] Cycle complete. Waiting 10 seconds for next OpenSky sweep...");
                    await Task.Delay(10000, context.CancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[C2 TELEMETRY SERVER] WPF Client closed the streaming channel safely.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C2 TELEMETRY SERVER] Critical stream error: {ex.Message}");
            }
        }

        // Python FastAPI'ye (8081) bağlanan ve Risk Skorunu çeken metot
        private async Task<float> GetRiskScoreFromPythonAsync(FlightDataModel flight)
        {
            try
            {
                // Python'daki BaseModel'in (FlightData) tam olarak beklediği alanlar:
                var payload = new
                {
                    flightno = flight.FlightNo,
                    velocity = flight.Velocity,
                    heading = flight.Heading,
                    vertrate = flight.VertRate,
                    baroaltitude = flight.BaroAltitude,
                    squawk = flight.Squawk,
                    alert = flight.Alert,
                    spi = flight.Spi,
                    latitude = flight.Latitude,
                    longitude = flight.Longitude
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Python'a POST isteği at
                var response = await _httpClient.PostAsync(_pythonMlUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultStr = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(resultStr))
                    {
                        // Python API'nin dönüşü: {"isAnomaly": true/false} (veya varsa bir mesaj)
                        // Senin Python kodunda "isAnomaly" dönüyor.
                        if (doc.RootElement.TryGetProperty("isAnomaly", out JsonElement anomalyElement))
                        {
                            bool isAnomaly = anomalyElement.GetBoolean();
                            return isAnomaly ? 85.0f : 12.0f; // WPF arayüzü için sembolik RiskScore.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Python sunucusu kapalıysa buraya düşer, konsolu kirletmemek için yoruma alabilirsin
                // Console.WriteLine($"[ML ERROR] Python engine connection failed: {ex.Message}");
            }

            // Hata olursa veya Python kapalıysa uçağı güvenli (Risk 0) kabul et
            return 0.0f;
        }

        // OpenSky API'den gelen karmaşık JSON'u temizleyen metot
        private List<FlightDataModel> ParseOpenSkyJson(string json)
        {
            var results = new List<FlightDataModel>();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty("states", out JsonElement statesElement) && statesElement.ValueKind != JsonValueKind.Null)
                {
                    foreach (JsonElement state in statesElement.EnumerateArray())
                    {
                        // OpenSky verisi dizi halindedir, en az 12 eleman olmalı
                        if (state.GetArrayLength() < 12) continue;

                        // Callsign boşsa ICAO kodunu kullan
                        var callsign = state[1].ValueKind == JsonValueKind.String ? state[1].GetString().Trim() : "";
                        if (string.IsNullOrEmpty(callsign)) callsign = state[0].GetString();

                        results.Add(new FlightDataModel
                        {
                            FlightNo = callsign,
                            Longitude = state[5].ValueKind == JsonValueKind.Number ? state[5].GetDouble() : 0,
                            Latitude = state[6].ValueKind == JsonValueKind.Number ? state[6].GetDouble() : 0,
                            BaroAltitude = (state[7].ValueKind == JsonValueKind.Number ? state[7].GetDouble() : 0) * 3.28084, // m to ft
                            Velocity = (state[9].ValueKind == JsonValueKind.Number ? state[9].GetDouble() : 0) * 1.94384, // m/s to kts
                            Heading = state[10].ValueKind == JsonValueKind.Number ? state[10].GetDouble() : 0,
                            VertRate = state[11].ValueKind == JsonValueKind.Number ? state[11].GetDouble() : 0,
                            Squawk = 0, // Şimdilik default 0
                            Alert = 0,
                            Spi = 0
                        });
                    }
                }
            }
            return results;
        }
    }

    public class FlightDataModel
    {
        public string FlightNo { get; set; }
        public double Velocity { get; set; }
        public double Heading { get; set; }
        public double VertRate { get; set; }
        public double BaroAltitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Squawk { get; set; }
        public int Alert { get; set; }
        public int Spi { get; set; }
    }
}