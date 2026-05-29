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
    [RoutePrefix("api/usuarios")]
    public class UsuariosController : ApiController
    {
        private UsuarioBLL _usuarioBLL =
            new UsuarioBLL(
                ConfigurationManager
                .ConnectionStrings["Northwind2ConnectionString"]
                .ConnectionString);

        [HttpPost]
        [Route("insertar")]
        [Permiso(7)]
        public async Task<IHttpActionResult> Insertar(Usuario usuario)
        {
            int numRegs = _usuarioBLL.Insertar(usuario);

            if (numRegs > 0)
            {
                await UsuarioNotifier.Insertado(
                    usuario.Id);
            }

            return Ok(new
            {
                NumRegs = numRegs,
                Usuario = usuario
            });
        }

        [HttpPut]
        [Route("actualizar")]
        [Permiso(7)]
        public async Task<IHttpActionResult> Actualizar(Usuario usuario)
        {
            int numRegs = _usuarioBLL.Actualizar(usuario);

            if (numRegs > 0)
            {
                await UsuarioNotifier.Actualizado(
                    usuario.Id);
            }

            return Ok( new
            { 
                numRegs, 
                usuario 
            });
        }

        [HttpDelete]
        [Route("eliminar/{id}")]
        [Permiso(7)]
        public async Task<IHttpActionResult> Eliminar(
            int id,
            [FromUri] string rowVersion)
        {
            var rowVersionBytes =
                Convert.FromBase64String(rowVersion);

            Usuario usuario = new Usuario
            {
                Id = id,
                RowVersion = rowVersionBytes
            };

            int numRegs =
                _usuarioBLL.Eliminar(
                    usuario);

            if (numRegs > 0)
            {
                await UsuarioNotifier.Eliminado(usuario.Id);
            }

            return Ok(numRegs);
        }
    }
}