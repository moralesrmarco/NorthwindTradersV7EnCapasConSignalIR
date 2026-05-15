using Entities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class ApiEmpleadoService
    {
        public static async Task<(bool ok, string mensaje, Empleado empleado)> InsertarAsync(Empleado empleado)
        {
            var response = await ApiService.PostAsync(
                "api/empleados/insertar",
                empleado);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content.ReadAsAsync<dynamic>();

                int numRegs = resultado.NumRegs;

                var empleadoInsertado =
                    JsonConvert.DeserializeObject<Empleado>(
                        resultado.Empleado.ToString());

                if (numRegs > 0)
                {
                    return (
                        true,
                        "Empleado insertado correctamente",
                        empleadoInsertado
                    );
                }

                return (
                    false,
                    "No se insertó ningún registro",
                    null
                );
            }

            //SesionActual.CerrarSesion();

            //return (
            //    false,
            //    $"Error API: {response.StatusCode}",
            //    null
            //);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SesionActual.CerrarSesion();
            }

            return (
                false,
                $"Error API: {response.StatusCode}",
                null
            );
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            ActualizarAsync(Empleado empleado)
        {
            var response = await ApiService.PutAsync(
                "api/empleados/actualizar",
                empleado);

            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            //SesionActual.CerrarSesion();

            //string mensaje =
            //    await response.Content.ReadAsStringAsync();

            //return (false, mensaje, 0);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SesionActual.CerrarSesion();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }

        public static async Task<(bool ok, string mensaje, int numRegs)>
            EliminarAsync(int employeeId, byte[] rowVersion)
        {
            string rowVersionBase64 =
                System.Convert.ToBase64String(rowVersion);

            var response = await ApiService.DeleteAsync(
                $"api/empleados/eliminar/{employeeId}?rowVersion={rowVersionBase64}");

            if (response.IsSuccessStatusCode)
            {
                int numRegs =
                    await response.Content.ReadAsAsync<int>();

                return (true, "", numRegs);
            }

            //SesionActual.CerrarSesion();

            //string mensaje =
            //    await response.Content.ReadAsStringAsync();

            //return (false, mensaje, 0);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                SesionActual.CerrarSesion();
            }

            string mensaje =
                await response.Content.ReadAsStringAsync();

            return (false, mensaje, 0);
        }
    }
}
