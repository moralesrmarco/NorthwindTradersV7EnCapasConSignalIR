using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiService
    {
        public static async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            using (var client = new HttpClient())
            {
                SesionActual.RefreshTokenExpirado = false;
                client.BaseAddress = new Uri(SesionActual.UrlBaseSignalR);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SesionActual.AccessToken);

                var response = await client.PostAsJsonAsync(url, data);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    bool refreshed =
                        await SesionActual.RefreshAccessTokenAsync(SesionActual.UrlBaseSignalR);

                    if (refreshed)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", SesionActual.AccessToken);

                        response = await client.PostAsJsonAsync(url, data);
                    }
                    else
                    {
                        SesionActual.RefreshTokenExpirado = true;
                    }
                }

                return response;
            }
        }

        public static async Task<HttpResponseMessage> PutAsync(
            string endpoint,
            object data)
        {
            using (var client = new HttpClient())
            {
                SesionActual.RefreshTokenExpirado = false;

                client.BaseAddress =
                    new Uri(SesionActual.UrlBaseSignalR);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        SesionActual.AccessToken);

                var response =
                    await client.PutAsJsonAsync(endpoint, data);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    bool refreshed =
                        await SesionActual.RefreshAccessTokenAsync(
                            SesionActual.UrlBaseSignalR);

                    if (refreshed)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue(
                                "Bearer",
                                SesionActual.AccessToken);

                        response =
                            await client.PutAsJsonAsync(endpoint, data);
                    }
                    else
                    {
                        SesionActual.RefreshTokenExpirado = true;
                    }
                }

                return response;
            }
        }

        public static async Task<HttpResponseMessage> DeleteAsync(
            string endpoint)
        {
            using (var client = new HttpClient())
            {
                SesionActual.RefreshTokenExpirado = false;

                client.BaseAddress =
                    new Uri(SesionActual.UrlBaseSignalR);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        SesionActual.AccessToken);

                var response =
                    await client.DeleteAsync(endpoint);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    bool refreshed =
                        await SesionActual.RefreshAccessTokenAsync(
                            SesionActual.UrlBaseSignalR);

                    if (refreshed)
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue(
                                "Bearer",
                                SesionActual.AccessToken);

                        response =
                            await client.DeleteAsync(endpoint);
                    }
                    else
                    {
                        SesionActual.RefreshTokenExpirado = true;
                    }
                }

                return response;
            }
        }
    }
}