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

//using System;
//using System.Configuration;
//using System.Net.Http;

//namespace NorthwindTradersV7EnCapasConSignalIR.Services
//{
//    public static class HttpClientProvider
//    {
//        public static readonly HttpClient Client =
//            new HttpClient();

//        static HttpClientProvider()
//        {
//            Client = new HttpClient
//            {
//                BaseAddress = new Uri(
//                    ConfigurationManager.AppSettings["UrlBaseSignalR"])
//            };
//        }
//    }
//}