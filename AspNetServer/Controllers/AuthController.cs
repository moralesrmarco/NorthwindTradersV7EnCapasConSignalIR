using BLL;
using Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private UsuarioBLL _usuarioBLL = new UsuarioBLL(ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString);

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(Usuario usuario)
        {
            var usuarioAuth = _usuarioBLL.ValidarUsuario(usuario);
            usuarioAuth.User = usuario.User;
            usuarioAuth.Password = usuario.Password;
            if (usuarioAuth != null && usuarioAuth.Id >0)
            {
                var key = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);
                var tokenHandler = new JwtSecurityTokenHandler();

                var descriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, usuarioAuth.User)
                    }),

                    //Expires = DateTime.UtcNow.AddHours(20),
                    Expires = DateTime.UtcNow.AddMinutes(10),

                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(descriptor);

                return Ok(new
                {
                    Token = tokenHandler.WriteToken(token),
                    Usuario = new
                    {
                        usuarioAuth.Id,
                        usuarioAuth.User,
                        usuarioAuth.Paterno,
                        usuarioAuth.Materno,
                        usuarioAuth.Nombres
                    }
                });
            }

            return Unauthorized();
        }
    }
}