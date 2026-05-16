using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class SessionService
    {
        public static async Task<bool> ValidarSesionAsync()
        {
            var response =
                await ApiService.GetAsync("api/auth/validarsesion");

            return response.IsSuccessStatusCode;
        }
    }
}