using AspNetServer.Seguridad;
using BLL;
using Entities;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [Authorize]
    [RoutePrefix("api/empleados")]
    public class EmpleadosController : ApiController
    {
        private EmpleadoBLL _empleadoBLL =
            new EmpleadoBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(1)]
        public async Task<IHttpActionResult> Insertar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Insertar(empleado);

            if (numRegs > 0)
            {
                await EmpleadoNotifier.Insertado(
                    empleado.EmployeeID);
            }

            return Ok(new
            {
                NumRegs = numRegs,
                Empleado = empleado
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(1)]
        public async Task<IHttpActionResult> Actualizar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Actualizar(empleado);

            if (numRegs > 0)
            {
                await EmpleadoNotifier.Actualizado(
                    empleado.EmployeeID);
            }

            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(1)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var rowVersionBytes =
                Convert.FromBase64String(rowVersion);

            int numRegs =
                _empleadoBLL.Eliminar(
                    id,
                    rowVersionBytes);

            if (numRegs > 0)
            {
                await EmpleadoNotifier.Eliminado(id);
            }

            return Ok(numRegs);
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Obtener(int id)
        {
            var empleado =
                _empleadoBLL.ObtenerEmpleadoPorId(id);

            if (empleado != null)
                return Ok(empleado);

            return NotFound();
        }
    }
}