using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiClienteService
    {
        public static async Task<(bool ok, string mensaje, Cliente cliente)>
            InsertarAsync(Cliente cliente)
        {
            var response = await ApiService.PostAsync(
                "api/clientes/insertar",
                cliente);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var clienteInsertado =
                    JsonConvert.DeserializeObject<Cliente>(
                        resultado.Cliente.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Cliente insertado correctamente",
                        clienteInsertado
                    );
                }

                return (
                    false,
                    "No se insertó ningún registro",
                    null
                );
            }

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                AppShutdownService.LogoutAndClose();
            }

            return (
                false,
                $"Error API: {response.StatusCode}",
                null
            );
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            ActualizarAsync(Cliente cliente)
        {
            var response = await ApiService.PutAsync(
                "api/clientes/actualizar",
                cliente);

            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            EliminarAsync(
                string customerId,
                byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            var response = await ApiService.DeleteAsync(
                $"api/clientes/eliminar/{customerId}?rowVersion={rowVersionBase64}");

            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }
    }
}