using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System.Configuration;
using System.Text;

[assembly: OwinStartup(typeof(AspNetServer.Startup))]

namespace AspNetServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 🔹 Configuración de autenticación JWT
            var key = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JWT_SECRET_KEY"]);

            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = false,
                    ValidateAudience = false,

                    ValidateLifetime = true,
                    ClockSkew = System.TimeSpan.Zero
                },

                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        var identity = context.Ticket.Identity;

                        var tokenType =
                            identity.FindFirst("TokenType")?.Value;

                        if (tokenType != "access")
                        {
                            context.SetError(
                                "invalid_token",
                                "Solo access tokens permitidos");
                            context.Rejected();
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    },

                    OnRequestToken = context =>
                    {
                        var token =
                            context.Request.Query.Get("access_token");

                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                }
            });

            // 🔹 Registro de hubs de SignalR
            app.MapSignalR();
        }
    }
}
