using AspNetServer.Hubs;
using BLL;
using Entities;
using Microsoft.AspNet.SignalR;
using System.Configuration;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    public class EmpleadosController : ApiController
    {
        private EmpleadoBLL _empleadoBLL = new EmpleadoBLL(ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString);

        [HttpPost]
        [Route("api/empleados/insertar")]
        public IHttpActionResult Insertar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Insertar(empleado);

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Insertado", empleado.EmployeeID);
                return Ok(numRegs);
            }

            return BadRequest("No se insertó el empleado");
        }

        [HttpPut]
        [Route("api/empleados/actualizar")]
        public IHttpActionResult Actualizar(Empleado empleado)
        {
            int numRegs = _empleadoBLL.Actualizar(empleado);

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Actualizado", empleado.EmployeeID);
                return Ok(numRegs);
            }

            return BadRequest("No se actualizó el empleado");
        }

        [HttpDelete]
        [Route("api/empleados/eliminar/{id}")]
        public IHttpActionResult Eliminar(int id)
        {
            int numRegs = _empleadoBLL.Eliminar(id, null); // ajusta RowVersion si lo usas

            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<EmpleadosHub>();
                context.Clients.All.empleadoActualizado("Eliminado", id);
                return Ok(numRegs);
            }

            return BadRequest("No se eliminó el empleado");
        }

        [HttpGet]
        [Route("api/empleados/{id}")]
        public IHttpActionResult Obtener(int id)
        {
            var empleado = _empleadoBLL.ObtenerEmpleadoPorId(id); 

            if (empleado != null)
            {
                return Ok(empleado);
            }

            return NotFound();
        }

    }
}