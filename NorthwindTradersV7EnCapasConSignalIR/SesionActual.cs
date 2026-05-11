using NorthwindTradersV7EnCapasConSignalIR.Services;
using System;
using System.Configuration;
using System.Net.Http;
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
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);

                var response = await client.PostAsJsonAsync(
                    "api/auth/refresh",
                    new { RefreshToken = RefreshToken });

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadAsAsync<dynamic>();

                    AccessToken = (string)result.AccessToken;

                    return true;
                }
            }

            return false;
        }

        //public static async Task<bool> RefreshAccessTokenAsync(string baseUrl)
        //{

        //    //string carpeta = Path.Combine(Application.StartupPath, "Logs");
        //    //if (!Directory.Exists(carpeta))
        //    //{
        //    //    Directory.CreateDirectory(carpeta);
        //    //}

        //    //string rutaLog = Path.Combine(carpeta, "log.txt");

        //    //if (!File.Exists(rutaLog))
        //    //{
        //    //    using (var fs = File.Create(rutaLog)) { }
        //    //}

        //    //File.AppendAllText(rutaLog, "Log inicial\n");


        //    using (var client = new HttpClient())
        //    {
        //        client.BaseAddress = new Uri(baseUrl);
        //        //try
        //        //{
        //        //    // Log de depuración
        //        //    System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [CLIENTE] RefreshToken actual: {RefreshToken}\n");
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    System.Diagnostics.Debug.WriteLine($"Error al escribir log: {ex.Message}");
        //        //}

        //        var response = await client.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = RefreshToken });

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var result = await response.Content.ReadAsAsync<dynamic>();
        //            AccessToken = (string)result.AccessToken;

        //            //System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [CLIENTE] Nuevo AccessToken recibido: {AccessToken}\n");

        //            return true;
        //        }
        //        //else
        //        //{

        //        //    System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [CLIENTE] Error al refrescar token: {response.StatusCode}\n");

        //        //}
        //    }
        //    return false;
        //}
    }
}
