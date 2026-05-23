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
    [RoutePrefix("api/categorias")]
    public class CategoriasController : ApiController
    {
        private CategoriaBLL _categoriaBLL =
            new CategoriaBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(4)]
        public async Task<IHttpActionResult> Insertar(Categoria categoria)
        {
            int numRegs = _categoriaBLL.Insertar(categoria);

            if (numRegs > 0)
            {
                await CategoriaNotifier.Insertado(
                    categoria.CategoryID);
            }

            return Ok(new
            {
                NumRegs = numRegs,
                Categoria = categoria
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(4)]
        public async Task<IHttpActionResult> Actualizar(Categoria categoria)
        {
            int numRegs = _categoriaBLL.Actualizar(categoria);

            if (numRegs > 0)
            {
                await CategoriaNotifier.Actualizado(
                    categoria.CategoryID);
            }

            return Ok(numRegs);
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(4)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var rowVersionBytes =
                Convert.FromBase64String(rowVersion);

            int numRegs =
                _categoriaBLL.Eliminar(
                    id,
                    rowVersionBytes);

            if (numRegs > 0)
            {
                await CategoriaNotifier.Eliminado(id);
            }

            return Ok(numRegs);
        }
    }
}