using System;
using System.Net.Http;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class HttpClientProvider
    {
        private static readonly HttpClient _client;

        static HttpClientProvider()
        {
            _client = new HttpClient();

            _client.BaseAddress =
                new Uri(SesionActual.UrlBaseSignalR);

            _client.Timeout =
                TimeSpan.FromSeconds(30);
        }

        public static HttpClient Client => _client;
    }
}