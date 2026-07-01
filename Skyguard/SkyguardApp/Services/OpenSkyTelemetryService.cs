using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SkyguardApp.Services
{
    // OpenSky'dan gelen veriyi senin sistemine uygun hale getiren taşıyıcı sınıf
    public class LiveFlightData
    {
        public string Icao24 { get; set; }
        public string Callsign { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double BaroAltitude { get; set; } // Metre cinsinden gelir, ft'ye çevireceğiz
        public double Velocity { get; set; }     // m/s cinsinden gelir, knot'a çevireceğiz
        public double Heading { get; set; }
    }

    public class OpenSkyTelemetryService
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;

        // Veri geldiğinde arayüzü (RadarView) tetikleyecek Event
        public event Action<List<LiveFlightData>> OnTelemetryBatchReceived;
        public event Action<string, string> OnSystemLog; // Tür, Mesaj

        // Türkiye Bounding Box Koordinatları (Yaklaşık)
        private readonly string _turkeyBoundingBox = "?lamin=35.8&lomin=25.6&lamax=42.1&lomax=44.8";

        public bool IsStreaming { get; private set; } = false;

        public OpenSkyTelemetryService()
        {
            _httpClient = new HttpClient();
            // OpenSky API, User-Agent header'ı olmadan istekleri reddedebilir
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SkyGuard_C2_Terminal/1.0");
        }

        public void StartStream()
        {
            if (IsStreaming) return;

            IsStreaming = true;
            _cancellationTokenSource = new CancellationTokenSource();

            OnSystemLog?.Invoke("SYSTEM", "OpenSky Network API connection established.");

            // Arka planda sürekli çalışacak Task
            Task.Run(() => FetchTelemetryLoop(_cancellationTokenSource.Token));
        }

        public void StopStream()
        {
            if (!IsStreaming) return;

            IsStreaming = false;
            _cancellationTokenSource?.Cancel();
            OnSystemLog?.Invoke("WARN", "OpenSky Telemetry stream disconnected by operator.");
        }

        private async Task FetchTelemetryLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string url = $"https://opensky-network.org/api/states/all{_turkeyBoundingBox}";

                    HttpResponseMessage response = await _httpClient.GetAsync(url, token);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResult = await response.Content.ReadAsStringAsync();
                        var parsedData = ParseOpenSkyJson(jsonResult);

                        if (parsedData.Count > 0)
                        {
                            OnSystemLog?.Invoke("INFO", $"OpenSky Data Ingest: {parsedData.Count} targets tracked in sector.");
                            OnTelemetryBatchReceived?.Invoke(parsedData);
                        }
                    }
                    else
                    {
                        OnSystemLog?.Invoke("THREAT", $"API HTTP Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    OnSystemLog?.Invoke("THREAT", $"Telemetry Fetch Exception: {ex.Message}");
                }

                // OpenSky anonim kullanıcılar için 10 saniye limit koyar. Yoksa IP ban atar.
                await Task.Delay(10000, token);
            }
        }

        private List<LiveFlightData> ParseOpenSkyJson(string json)
        {
            var results = new List<LiveFlightData>();
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("states", out JsonElement statesElement) && statesElement.ValueKind != JsonValueKind.Null)
                    {
                        foreach (JsonElement state in statesElement.EnumerateArray())
                        {
                            // OpenSky veri dizilimi sabit indekslidir
                            if (state.GetArrayLength() < 10) continue;

                            var callsign = state[1].ValueKind == JsonValueKind.String ? state[1].GetString().Trim() : "UNKNOWN";
                            if (string.IsNullOrEmpty(callsign)) callsign = "GHOST_TRACK";

                            // Gerekli verileri ayıkla ve dönüştür
                            var track = new LiveFlightData
                            {
                                Icao24 = state[0].GetString(),
                                Callsign = callsign,
                                Longitude = state[5].ValueKind == JsonValueKind.Number ? state[5].GetDouble() : 0,
                                Latitude = state[6].ValueKind == JsonValueKind.Number ? state[6].GetDouble() : 0,

                                // Metre -> Feet dönüşümü (1 m = 3.28084 ft)
                                BaroAltitude = (state[7].ValueKind == JsonValueKind.Number ? state[7].GetDouble() : 0) * 3.28084,

                                // m/s -> Knot dönüşümü (1 m/s = 1.94384 knot)
                                Velocity = (state[9].ValueKind == JsonValueKind.Number ? state[9].GetDouble() : 0) * 1.94384,

                                Heading = state[10].ValueKind == JsonValueKind.Number ? state[10].GetDouble() : 0
                            };

                            results.Add(track);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON Parse Error: {ex.Message}");
            }

            return results;
        }
    }
}