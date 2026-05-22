using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiProveedorService
    {
        public static async Task<(bool ok, string mensaje, Proveedor proveedor)> InsertarAsync(Proveedor proveedor)
        {
            var response = await ApiService.PostAsync(
                "api/proveedores/insertar",
                proveedor);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var proveedorInsertado =
                    JsonConvert.DeserializeObject<Proveedor>(
                        resultado.Proveedor.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Proveedor insertado correctamente",
                        proveedorInsertado
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
            ActualizarAsync(Proveedor proveedor)
        {
            var response = await ApiService.PutAsync(
                "api/proveedores/actualizar",
                proveedor);
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
            EliminarAsync(int supplierId, byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            string encoded =
                System.Net.WebUtility.UrlEncode(rowVersionBase64);

            var response = await ApiService.DeleteAsync(
                $"api/proveedores/eliminar/{supplierId}?rowVersion={encoded}");

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
