using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Skyguard.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string FastApiUrl = "http://127.0.0.1:8081/Login";

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<IActionResult> Login()
        {
            try
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                string rawJsonBody = await reader.ReadToEndAsync();
                
                System.Diagnostics.Debug.WriteLine($"[Skyguard API] Ham İstek: {rawJsonBody}");

                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(rawJsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(FastApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string successContent = await response.Content.ReadAsStringAsync();
                    return Content(successContent, "application/json");
                }

                string errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"C# API Köprü Hatası: {ex.Message}");
            }
        }
    }
}