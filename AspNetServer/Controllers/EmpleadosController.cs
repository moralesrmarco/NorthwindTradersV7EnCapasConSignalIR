using AspNetServer.Hubs;
using BLL;
using Entities;
using Microsoft.AspNet.SignalR;
using System;
using System.Configuration;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [RoutePrefix("api/empleados")]
    public class EmpleadosController : ApiController
    {
        private EmpleadoBLL _empleadoBLL = new EmpleadoBLL(ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString);

        [HttpPost]
        [Route("insertar")]
        public IHttpActionResult Insertar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Insertar(empleado);

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Insertado", empleado.EmployeeID);
            }
            return Ok(numRegs);
        }

        [HttpPut]
        [Route("actualizar")]
        public IHttpActionResult Actualizar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Actualizar(empleado);

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Actualizado", empleado.EmployeeID);
            }
            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        public IHttpActionResult Eliminar(int id, [FromUri] string rowVersion)
        {
            var rowVersionBytes = Convert.FromBase64String(rowVersion);
            int numRegs = _empleadoBLL.Eliminar(id, rowVersionBytes);

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Eliminado", id);
            }
            return Ok(numRegs);
        }


        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Obtener(int id)
        {
            var empleado = _empleadoBLL.ObtenerEmpleadoPorId(id);
            if (empleado != null)
                return Ok(empleado);
            return NotFound();
        }
    }
}