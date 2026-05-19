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
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private readonly UsuarioBLL _usuarioBLL;
        private readonly PermisoService _permisoService;

        public AuthController()
        {
            _usuarioBLL = new UsuarioBLL(_connectionString);
            _permisoService = new PermisoService(_connectionString);
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(Usuario usuario)
        {
            if (usuario == null)
                return BadRequest("Datos inválidos");

            var usuarioAuth = _usuarioBLL.ValidarUsuario(usuario);

            if (usuarioAuth == null || usuarioAuth.Id <= 0)
            {
                return Unauthorized();
            }

            // CARGAR PERMISOS DEL USUARIO
            usuarioAuth.PermisosIds =
                _permisoService.ObtenerPermisosPorUsuarioId(usuarioAuth.Id);

            var accessMinutes = ObtenerAccessMinutes();

            var refreshExpiration = ObtenerExpiracionRefreshToken();

            usuarioAuth.User = usuario.User;

            // =========================
            // ACCESS TOKEN
            // =========================
            var accessToken =
                CrearToken(
                    usuarioAuth,
                    "access",
                    DateTime.UtcNow.AddMinutes(
                        accessMinutes));

            // =========================
            // REFRESH TOKEN
            // =========================
            var refreshToken =
                CrearToken(
                    usuarioAuth,
                    "refresh",
                    refreshExpiration);

            return Ok(new
            {
                AccessToken =
                    _tokenHandler.WriteToken(accessToken),

                RefreshToken =
                    _tokenHandler.WriteToken(refreshToken),

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

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh([FromBody] RefreshRequest request)
        {
            var accessMinutes = ObtenerAccessMinutes();

            if (request == null ||
            string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("RefreshToken requerido");

            var refreshToken = request.RefreshToken;

            var key = ObtenerKey();

            try
            {
                // VALIDAR REFRESH TOKEN
                _tokenHandler.ValidateToken(
                    refreshToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        ValidateIssuer = true,
                        ValidateAudience = true,

                        ValidIssuer =
                            ConfigurationManager.AppSettings["JWT_ISSUER"],

                        ValidAudience =
                            ConfigurationManager.AppSettings["JWT_AUDIENCE"],

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
                    SecurityAlgorithms.HmacSha256Signature)
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
                var newAccessToken =
                    CrearToken(
                        usuario,
                        "access",
                        DateTime.UtcNow.AddMinutes(
                            accessMinutes));

                return Ok(new
                {
                    AccessToken =
                        _tokenHandler.WriteToken(newAccessToken)
                });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
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

        // =========================
        // MÉTODOS AUXILIARES
        // =========================

        private bool EsProduccion()
        {
            return bool.Parse(
                ConfigurationManager.AppSettings["JWT_IS_PRODUCTION"]);
        }

        private int ObtenerAccessMinutes()
        {
            return EsProduccion()
                ? int.Parse(
                    ConfigurationManager.AppSettings["JWT_ACCESS_MINUTES_PROD"])
                : int.Parse(
                    ConfigurationManager.AppSettings["JWT_ACCESS_MINUTES"]);
        }

        private DateTime ObtenerExpiracionRefreshToken()
        {
            return EsProduccion()
                ? DateTime.UtcNow.AddDays(
                    int.Parse(
                        ConfigurationManager.AppSettings["JWT_REFRESH_DAYS"]))
                : DateTime.UtcNow.AddMinutes(
                    int.Parse(
                        ConfigurationManager.AppSettings["JWT_REFRESH_MINUTES"]));
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

        private SecurityToken CrearToken(
            Usuario usuario,
            string tokenType,
            DateTime expiracion)
        {
            var key = ObtenerKey();

            var descriptor =
                new SecurityTokenDescriptor
                {
                    Subject =
                        new ClaimsIdentity(
                            ObtenerClaims(usuario, tokenType)),

                    Expires = expiracion,

                    Issuer =
                        ConfigurationManager.AppSettings["JWT_ISSUER"],

                    Audience =
                        ConfigurationManager.AppSettings["JWT_AUDIENCE"],

                    SigningCredentials =
                        new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha256Signature)
                };

            return _tokenHandler.CreateToken(descriptor);
        }

        private readonly JwtSecurityTokenHandler _tokenHandler =
            new JwtSecurityTokenHandler();

        private byte[] ObtenerKey()
        {
            return Encoding.UTF8.GetBytes(
                ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);
        }
    }
}