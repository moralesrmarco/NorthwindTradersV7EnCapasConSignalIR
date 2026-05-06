using AspNetServer.Hubs;
using BLL;
using Entities;
using Microsoft.AspNet.SignalR;
using System;
using System.Configuration;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [RoutePrefix("api/clientes")]
    public class ClientesController : ApiController
    {
        private ClienteBLL _clienteBLL = new ClienteBLL(ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString);

        [HttpPost]
        [Route("insertar")]
        public IHttpActionResult Insertar(Cliente cliente)
        {
            int numRegs = _clienteBLL.Insertar(cliente);
            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<ClientesHub>();
                context.Clients.All.clienteActualizado("Insertado", cliente.CustomerID);
            }
            // Devuelves un objeto anónimo con ambos valores
            return Ok(new { NumRegs = numRegs, Cliente = cliente });
        }

        [HttpPut]
        [Route("actualizar")]
        public IHttpActionResult Actualizar(Cliente cliente)
        {
            int numRegs = _clienteBLL.Actualizar(cliente);
            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<ClientesHub>();
                context.Clients.All.clienteActualizado("Actualizado", cliente.CustomerID);
            }
            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        public IHttpActionResult Eliminar(string id, [FromUri] string rowVersion)
        {
            var rowVersionBytes = Convert.FromBase64String(rowVersion);
            int numRegs = _clienteBLL.Eliminar(id, rowVersionBytes);
            if (numRegs > 0)
            {
                var context = GlobalHost.ConnectionManager.GetHubContext<ClientesHub>();
                context.Clients.All.clienteActualizado("Eliminado", id);
            }
            return Ok(numRegs);
        }
    }
}