using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiService
    {
        private static async Task<HttpResponseMessage> SendAsync(
            Func<HttpClient, Task<HttpResponseMessage>> sendRequest,
            bool retrying = false)
        {
            var client = HttpClientProvider.Client;

            SesionActual.RefreshTokenExpirado = false;

            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SesionActual.AccessToken);

            var response = await sendRequest(client);

            // 401 → intentar refresh
            if (response.StatusCode == HttpStatusCode.Unauthorized && !retrying)
            {
                bool refreshed = await SesionActual.RefreshAccessTokenAsync(
                    SesionActual.UrlBaseSignalR);

                if (refreshed)
                {
                    client.DefaultRequestHeaders.Authorization = null;
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", SesionActual.AccessToken);

                    return await SendAsync(sendRequest, true);
                }
                else
                {
                    // refresh falló → cerrar app
                    SesionActual.RefreshTokenExpirado = true;
                    AppShutdownService.LogoutAndClose();
                    return response;
                }
            }

            // 403 → acceso prohibido (no hay recuperación posible)
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                AppShutdownService.LogoutAndClose();
            }

            return response;
        }

        public static async Task<HttpResponseMessage> PostAsync<T>(
            string endpoint,
            T data)
        {
            return await SendAsync(
                client => client.PostAsJsonAsync(endpoint, data));
        }

        public static async Task<HttpResponseMessage> PutAsync<T>(
            string endpoint,
            T data)
        {
            return await SendAsync(
                client => client.PutAsJsonAsync(endpoint, data));
        }

        public static async Task<HttpResponseMessage> DeleteAsync(
            string endpoint)
        {
            return await SendAsync(
                client => client.DeleteAsync(endpoint));
        }

        public static async Task<HttpResponseMessage> GetAsync(
            string endpoint)
        {
            return await SendAsync(
                client => client.GetAsync(endpoint));
        }
    }
}