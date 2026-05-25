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
    [RoutePrefix("api/productos")]
    public class ProductosController : ApiController
    {
        private ProductoBLL _productoBLL =
            new ProductoBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(5)]
        public async Task<IHttpActionResult> Insertar(Producto producto)
        {
            int numRegs = _productoBLL.Insertar(producto);

            if (numRegs > 0)
            {
                await ProductoNotifier.Insertado(
                    producto.ProductID);
            }

            return Ok(new
            {
                NumRegs = numRegs,
                Producto = producto
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(5)]
        public async Task<IHttpActionResult> Actualizar(Producto producto)
        {
            int numRegs = _productoBLL.Actualizar(producto);

            if (numRegs > 0)
            {
                await ProductoNotifier.Actualizado(
                    producto.ProductID);
            }

            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(5)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var rowVersionBytes =
                Convert.FromBase64String(rowVersion);

            int numRegs =
                _productoBLL.Eliminar(
                    id,
                    rowVersionBytes);

            if (numRegs > 0)
            {
                await ProductoNotifier.Eliminado(id);
            }

            return Ok(numRegs);
        }
    }
}