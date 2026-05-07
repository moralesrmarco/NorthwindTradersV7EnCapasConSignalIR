using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
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
                }
            });

            // 🔹 Registro de hubs de SignalR
            app.MapSignalR();
        }
    }
}
