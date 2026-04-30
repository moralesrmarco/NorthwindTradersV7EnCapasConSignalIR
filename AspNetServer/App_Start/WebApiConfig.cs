using System.Web.Http;

namespace AspNetServer.App_Start
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Habilitar rutas por atributos
            config.MapHttpAttributeRoutes();

            // Ruta por defecto (opcional, útil para GET api/empleados/5)
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Forzar salida JSON incluso en navegador
            config.Formatters.JsonFormatter.SupportedMediaTypes
                .Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/html"));
        }
    }
}