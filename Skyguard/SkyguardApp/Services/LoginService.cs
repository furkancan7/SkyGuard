using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// Klasör yapın SkyguardApp/Services olduğu için namespace bu olmalı:
namespace SkyGuardApp.Services
{
    public class LoginService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "http://127.0.0.1:8080/api/auth/login";

        public LoginService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<LoginResult?> AuthenticateAsync(string username, string password)
        {
            // C# tarafında küçük harfle ("username", "password") beklediğimiz için tam eşliyoruz
            var loginData = new { username = username, password = password };

            try
            {
                string targetUrl = "http://127.0.0.1:8080/api/auth/login";
                var response = await _httpClient.PostAsJsonAsync(targetUrl, loginData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResult>();
                }

                // Hata durumunda dönen HTTP kodunu (BadRequest gibi) ekranda görebilmek için burayı güncelledik
                return new LoginResult { status = "error", message = "Giriş başarısız! Sunucu kodu: " + response.StatusCode };
            }
            catch (Exception ex)
            {
                return new LoginResult { status = "error", message = "Sunucuya bağlanılamadı: " + ex.Message };
            }
        }
    }

    public class LoginResult
    {
        // C# 8+ ve .NET 8 için null uyarılarını engellemek adına varsayılan boş string atıyoruz
        public string status { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string redirect { get; set; } = string.Empty;
    }
}