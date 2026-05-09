using AspNetServer.Models;
using BLL;
using Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
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

                // Access token corto (10 min)
                var accessDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.Name, usuarioAuth.User)
            }),
                    Expires = DateTime.UtcNow.AddMinutes(2), // ojo
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var accessToken = tokenHandler.CreateToken(accessDescriptor);

                // Refresh token como JWT largo (7 días)
                var refreshDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, usuarioAuth.User)
                    }),
                    //Expires = DateTime.UtcNow.AddDays(1), // ojo
                    Expires = DateTime.UtcNow.AddMinutes(10), // ojo
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var refreshToken = tokenHandler.CreateToken(refreshDescriptor);

                return Ok(new

                {
                    AccessToken = tokenHandler.WriteToken(accessToken),
                    RefreshToken = tokenHandler.WriteToken(refreshToken),
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

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh([FromBody] RefreshRequest request)
        {


            string carpeta = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
            if (!System.IO.Directory.Exists(carpeta))
            {
                System.IO.Directory.CreateDirectory(carpeta);
            }

            string rutaLog = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/log.txt");

            if (!System.IO.File.Exists(rutaLog))
            {
                using (var fs = System.IO.File.Create(rutaLog)) { }
            }
            System.IO.File.AppendAllText(rutaLog, "Log inicial\n");

            System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] RefreshToken recibido: {request.RefreshToken}\n");

            var refreshToken = request.RefreshToken;
            var key = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // Validar firma y expiración del refresh token
                tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Si no se validó correctamente, devolvemos 401
                if (validatedToken == null)
                {
                    System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] RefreshToken inválido\n");
                    return Unauthorized();
                }

                var jwtToken = (JwtSecurityToken)validatedToken;
                //var username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var username = jwtToken.Claims.FirstOrDefault(c =>
                    c.Type == JwtRegisteredClaimNames.UniqueName || c.Type == ClaimTypes.Name
                )?.Value;
                if (string.IsNullOrEmpty(username))
                {
                    System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] RefreshToken sin usuario\n");
                    return Unauthorized();
                }

                System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] RefreshToken válido para usuario: {username}\n");

                // Generar nuevo access token
                var descriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
                    Expires = DateTime.UtcNow.AddMinutes(10),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var newAccessToken = tokenHandler.CreateToken(descriptor);

                System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] Nuevo AccessToken emitido: {tokenHandler.WriteToken(newAccessToken)}\n");

                return Ok(new { AccessToken = tokenHandler.WriteToken(newAccessToken) });
            }
            catch (Exception ex)
            {

                System.IO.File.AppendAllText(rutaLog, $"[{DateTime.Now}] [SERVIDOR] Error validando RefreshToken: {ex.Message}\n");
                
                return Unauthorized();
            }
        }
    }
}