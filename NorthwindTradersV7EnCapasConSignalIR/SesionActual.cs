using Newtonsoft.Json;
using NorthwindTradersV7EnCapasConSignalIR.Services;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public static class SesionActual
    {
        public static string UrlBaseSignalR { get; set; } = ConfigurationManager.AppSettings["UrlBaseSignalR"];

        // Token JWT que devuelve el API al hacer login
        public static string AccessToken { get; set; }

        public static string RefreshToken { get; set; }

        // Usuario autenticado (opcional, si quieres guardar más datos)
        public static string Usuario { get; set; }

        public static bool RefreshTokenExpirado { get; set; }

        public static async Task<bool> RefreshAccessTokenAsync(string baseUrl)
        {
            var client =
                HttpClientProvider.Client;

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/auth/refresh");

            var json =
                JsonConvert.SerializeObject(new
                {
                    RefreshToken = RefreshToken
                });

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result =
                await response.Content.ReadAsAsync<dynamic>();

            AccessToken =
                (string)result.AccessToken;

            return true;
        }

        public static void CerrarSesion()
        {
            AccessToken = null;
            RefreshToken = null;
            Usuario = null;
            RefreshTokenExpirado = false;
        }
    }
}
