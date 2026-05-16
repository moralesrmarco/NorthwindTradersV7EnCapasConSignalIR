using AspNetServer.Models;
using BLL;
using BLL.Services;
using Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web.Http;

namespace AspNetServer.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private UsuarioBLL _usuarioBLL;
        private PermisoService _permisoService;

        public AuthController()
        {
            _usuarioBLL = new UsuarioBLL(_connectionString);
            _permisoService = new PermisoService(_connectionString);
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(Usuario usuario)
        {
            var usuarioAuth = _usuarioBLL.ValidarUsuario(usuario);

            if (usuarioAuth == null || usuarioAuth.Id <= 0)
            {
                return Unauthorized();
            }

            // CARGAR PERMISOS DEL USUARIO
            usuarioAuth.PermisosIds =
                _permisoService.ObtenerPermisosPorUsuarioId(usuarioAuth.Id);

            var accessMinutes =
                int.Parse(ConfigurationManager.AppSettings["JWT_ACCESS_MINUTES"]);

            var refreshMinutes =
                int.Parse(ConfigurationManager.AppSettings["JWT_REFRESH_MINUTES"]);

            usuarioAuth.User = usuario.User;

            var key = Encoding.UTF8.GetBytes(
                ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);

            var tokenHandler = new JwtSecurityTokenHandler();

            // =========================
            // ACCESS TOKEN
            // =========================
            var accessDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    ObtenerClaims(usuarioAuth, "access")
                ),

                Expires = DateTime.UtcNow.AddMinutes(accessMinutes),

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var accessToken =
                tokenHandler.CreateToken(accessDescriptor);

            // =========================
            // REFRESH TOKEN
            // =========================
            var refreshDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    ObtenerClaims(usuarioAuth, "refresh")
                ),

                Expires = DateTime.UtcNow.AddMinutes(refreshMinutes),

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var refreshToken =
                tokenHandler.CreateToken(refreshDescriptor);

            return Ok(new
            {
                AccessToken =
                    tokenHandler.WriteToken(accessToken),

                RefreshToken =
                    tokenHandler.WriteToken(refreshToken),

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

        private IEnumerable<Claim> ObtenerClaims(
            Usuario usuario,
            string tokenType)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Name,
                    usuario.User),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.Id.ToString()),

                new Claim(
                    "TokenType",
                    tokenType)
            };

            // SOLO ACCESS TOKEN LLEVA PERMISOS
            if (tokenType == "access")
            {
                foreach (var permisoId in usuario.PermisosIds)
                {
                    claims.Add(
                        new Claim(
                            "Permiso",
                            permisoId.ToString()));
                }
            }

            return claims;
        }

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh([FromBody] RefreshRequest request)
        {
            var accessMinutes =
                int.Parse(ConfigurationManager.AppSettings["JWT_ACCESS_MINUTES"]);

            if (request == null ||
            string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("RefreshToken requerido");
            }

            var refreshToken = request.RefreshToken;

            var key = Encoding.UTF8.GetBytes(
                ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // VALIDAR REFRESH TOKEN
                tokenHandler.ValidateToken(
                    refreshToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        ValidateIssuer = false,
                        ValidateAudience = false,

                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,

                        ClockSkew = TimeSpan.Zero
                    },
                    out SecurityToken validatedToken);

                // VALIDAR QUE SEA JWT
                if (!(validatedToken is JwtSecurityToken jwtSecurityToken))
                {
                    return Unauthorized();
                }

                // VALIDAR ALGORITMO
                if (jwtSecurityToken.Header.Alg !=
                    SecurityAlgorithms.HmacSha256)
                {
                    return Unauthorized();
                }

                // VALIDAR TOKEN TYPE
                var tokenType =
                    jwtSecurityToken.Claims.FirstOrDefault(c =>
                        c.Type == "TokenType")?.Value;

                if (tokenType != "refresh")
                {
                    return Unauthorized();
                }

                // OBTENER USERNAME
                var username =
                    jwtSecurityToken.Claims.FirstOrDefault(c =>
                        c.Type == JwtRegisteredClaimNames.UniqueName ||
                        c.Type == ClaimTypes.Name
                    )?.Value;

                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized();
                }

                // BUSCAR USUARIO EN BD
                var usuario =
                    _usuarioBLL.ObtenerPorUsername(username);

                if (usuario == null)
                {
                    return Unauthorized();
                }

                // RECARGAR PERMISOS
                usuario.PermisosIds =
                    _permisoService.ObtenerPermisosPorUsuarioId(
                        usuario.Id);

                // GENERAR NUEVO ACCESS TOKEN
                var claims =
                    ObtenerClaims(usuario, "access");

                var descriptor =
                    new SecurityTokenDescriptor
                    {
                        Subject =
                            new ClaimsIdentity(claims),

                        Expires =
                            DateTime.UtcNow.AddMinutes(
                                accessMinutes),

                        SigningCredentials =
                            new SigningCredentials(
                                new SymmetricSecurityKey(key),
                                SecurityAlgorithms.HmacSha256Signature)
                    };

                var newAccessToken =
                    tokenHandler.CreateToken(descriptor);

                return Ok(new
                {
                    AccessToken =
                        tokenHandler.WriteToken(newAccessToken)
                });
            }
            catch (Exception)
            {
                return Unauthorized();
            }
        }

        [Authorize]
        [HttpGet]
        [Route("validarsesion")]
        public IHttpActionResult ValidarSesion()
        {
            return Ok(new
            {
                ok = true,
                usuario = User.Identity.Name
            });
        }
    }
}