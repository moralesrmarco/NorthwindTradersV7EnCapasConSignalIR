using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiProductoService
    {
        public static async Task<(bool ok, string mensaje, Producto producto)> InsertarAsync(Producto producto)
        {
            var response = await ApiService.PostAsync(
                "api/productos/insertar",
                producto);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var productoInsertado =
                    JsonConvert.DeserializeObject<Producto>(
                        resultado.Producto.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Producto insertado correctamente",
                        productoInsertado
                    );
                }

                return (
                    false,
                    "No se insertó ningún registro",
                    null
                );
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            return (
                false,
                $"Error API: {response.StatusCode}",
                null
            );
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            ActualizarAsync(Producto producto)
        {
            var response = await ApiService.PutAsync(
                "api/productos/actualizar",
                producto);
            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            EliminarAsync(int productoId, byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            string encoded =
                System.Net.WebUtility.UrlEncode(rowVersionBase64);

            var response = await ApiService.DeleteAsync(
                $"api/productos/eliminar/{productoId}?rowVersion={encoded}");

            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }
    }
}
