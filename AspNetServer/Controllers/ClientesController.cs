using AspNetServer.Notifications;
using AspNetServer.Seguridad;
using BLL;
using Entities;
using System;
using System.Configuration;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [Authorize]
    [RoutePrefix("api/clientes")]
    public class ClientesController : ApiController
    {
        private ClienteBLL _clienteBLL = new ClienteBLL(ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(2)]
        public IHttpActionResult Insertar(Cliente cliente)
        {
            int numRegs = _clienteBLL.Insertar(cliente);
            if (numRegs > 0)
            {
                ClienteNotifier.Insertado(
                cliente.CustomerID);
            }
            // Devuelves un objeto anónimo con ambos valores
            return Ok(new { NumRegs = numRegs, Cliente = cliente });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(2)]
        public IHttpActionResult Actualizar(Cliente cliente)
        {
            int numRegs = _clienteBLL.Actualizar(cliente);
            if (numRegs > 0)
            {
                ClienteNotifier.Actualizado(
                    cliente.CustomerID);
            }
            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(2)]
        public IHttpActionResult Eliminar(string id, [FromUri] string rowVersion)
        {
            var rowVersionBytes = Convert.FromBase64String(rowVersion);
            int numRegs = _clienteBLL.Eliminar(id, rowVersionBytes);
            if (numRegs > 0)
            {
                ClienteNotifier.Eliminado(id);
            }
            return Ok(numRegs);
        }
    }
}