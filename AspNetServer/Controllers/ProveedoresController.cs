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
    [RoutePrefix("api/proveedores")]
    public class ProveedoresController : ApiController
    {
        private ProveedorBLL _proveedorBLL =
            new ProveedorBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(3)]
        public async Task<IHttpActionResult> Insertar(Proveedor proveedor)
        {
            int numRegs = _proveedorBLL.Insertar(proveedor);

            if (numRegs > 0)
                await ProveedorNotifier.Insertado(
                    proveedor.SupplierID);

            return Ok(new
            {
                NumRegs = numRegs,
                Proveedor = proveedor
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(3)]
        public async Task<IHttpActionResult> Actualizar(Proveedor proveedor)
        {
            int numRegs = _proveedorBLL.Actualizar(proveedor);

            if (numRegs > 0)
            {
                await ProveedorNotifier.Actualizado(
                    proveedor.SupplierID);
            }

            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(3)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var rowVersionBytes =
                Convert.FromBase64String(rowVersion);

            int numRegs =
                _proveedorBLL.Eliminar(
                    id,
                    rowVersionBytes);

            if (numRegs > 0)
            {
                await EmpleadoNotifier.Eliminado(id);
            }

            return Ok(numRegs);
        }
    }
}