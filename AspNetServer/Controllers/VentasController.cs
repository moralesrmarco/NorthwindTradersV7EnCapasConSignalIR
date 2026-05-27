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
    [RoutePrefix("api/ventas")]
    public class VentasController : ApiController
    {
        private VentaBLL _ventaBLL =
            new VentaBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(6)]
        public async Task<IHttpActionResult> Insertar(Venta venta)
        {
            int orderId;
            byte[] rowVersion;

            int numRegs = _ventaBLL.InsertarVentaCompleta(venta, out orderId, out rowVersion);

            if (numRegs > 0)
            {
                venta.OrderID = orderId;
                venta.RowVersion = rowVersion;
                await VentaNotifier.Insertado(
                    orderId);
            }

            return Ok(new
            {
                NumRegs = numRegs,
                Venta = venta
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(6)]
        public async Task<IHttpActionResult> Actualizar(Venta venta)
        {
            int numRegs = _ventaBLL.Actualizar(venta);

            if (numRegs > 0)
            {
                await VentaNotifier.Actualizado(
                    venta.OrderID);
            }

            return Ok(new
            {
                numRegs = numRegs,
                venta = venta
            });
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(6)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var venta = new Venta
            {
                OrderID = id,
                RowVersion = Convert.FromBase64String(rowVersion)
            };

            int numRegs = _ventaBLL.Eliminar(venta, out string productoExcede);

            if (numRegs > 0)
            {
                await VentaNotifier.Eliminado(id);
            }

            return Ok(new
            {
                numRegs = numRegs,
                productoExcede = productoExcede
            });
        }
    }
}