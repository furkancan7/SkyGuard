using Grpc.Core;
using Skyguard.Grpc;
using System.Net.Http.Json;

namespace SkyGuardBackend.Services;

public class FlightTrackerService : Skyguard.Grpc.FlightTrackerService.FlightTrackerServiceBase
{
    private readonly HttpClient _httpClient;

    public FlightTrackerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<FlightReporter> CheckAnomaly(FlightTracker request, ServerCallContext context)
    {
        FlightReporter finalResponse = new FlightReporter();
        string pythonModelUrl = "http://127.0.0.1:8081/predict";

        try
        {
            
            string jsonPayload = $@"{{
                ""flightno"": ""{request.Flightno}"",
                ""velocity"": {request.Velocity.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""heading"": {request.Heading.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""vertrate"": {request.Vertrate.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""baroaltitude"": {request.Baroaltitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                ""squawk"": {request.Squawk},
                ""alert"": {request.Alert},
                ""spi"": {request.Spi}
            }}";

            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            var responseMessage = await _httpClient.PostAsync(pythonModelUrl, content);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            finalResponse.IsAnomaly = responseString.Contains("true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Python modeline ulaşılamadı: {ex.Message}");
            finalResponse.IsAnomaly = false;
        }

        return finalResponse;
    }
}

public class PythonModelResponse
{
    public bool IsAnomaly { get; set; }
}
//"http://127.0.0.1:8000/predict" geert apı kodu!! 