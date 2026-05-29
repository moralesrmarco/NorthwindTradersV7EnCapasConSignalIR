using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiUsuarioService
    {
        public static async Task<(bool ok, string mensaje, Usuario usuario)> InsertarAsync(Usuario usuario)
        {
            var response = await ApiService.PostAsync(
                "api/usuarios/insertar",
                usuario);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var usuarioInsertado =
                    JsonConvert.DeserializeObject<Usuario>(
                        resultado.Usuario.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Usuario insertado correctamente",
                        usuarioInsertado
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

        public static async Task<(bool ok, string mensaje, int numRegs, Usuario usuario)>
            ActualizarAsync(Usuario usuario)
        {
            var response = await ApiService.PutAsync(
                "api/usuarios/actualizar",
                usuario);
            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.numRegs;

                var usuarioActualizado =
                    JsonConvert.DeserializeObject<Usuario>(
                        resultado.usuario.ToString());

                return (true, "", numRegs, usuarioActualizado);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AppShutdownService.LogoutAndClose();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0, null);
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            EliminarAsync(int usuarioId, byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            string encoded =
                System.Net.WebUtility.UrlEncode(rowVersionBase64);

            var response = await ApiService.DeleteAsync(
                $"api/usuarios/eliminar/{usuarioId}?rowVersion={encoded}");

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
