using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiVentaService
    {
        public static async Task<(bool ok, string mensaje, Venta venta)> InsertarAsync(Venta venta)
        {
            var response = await ApiService.PostAsync(
                "api/ventas/insertar",
                venta);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var ventaInsertada =
                    JsonConvert.DeserializeObject<Venta>(
                        resultado.Venta.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Venta insertada correctamente",
                        ventaInsertada
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

        public static async Task<(bool ok, string mensaje, int numRegs, Venta venta)>
            ActualizarAsync(Venta venta)
        {
            var response = await ApiService.PutAsync(
                "api/ventas/actualizar",
                venta);
            if (response.IsSuccessStatusCode)
            {
                    var resultado =
                        await response.Content.ReadAsAsync<dynamic>();
    
                    int numRegs = resultado.numRegs;

                   var ventaActualizada =
                        JsonConvert.DeserializeObject<Venta>(
                            resultado.venta.ToString());

                return (
                        true,
                        "",
                        numRegs,
                        ventaActualizada
                    );
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0, null);
        }

        public static async Task<(bool ok, string mensaje, int numRegs, string productoExcede)>
            EliminarAsync(int ventaId, byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            string encoded =
                System.Net.WebUtility.UrlEncode(rowVersionBase64);

            var response = await ApiService.DeleteAsync(
                $"api/ventas/eliminar/{ventaId}?rowVersion={encoded}");

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                return (
                            true,
                            "",
                            (int)resultado.numRegs,
                            (string)resultado.productoExcede
                        );
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (
                        false,
                        mensaje,
                        0,
                        ""
                   );
        }
    }
}
