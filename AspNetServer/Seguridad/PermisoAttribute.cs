using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace AspNetServer.Seguridad
{
    public class PermisoAttribute : AuthorizeAttribute
    {
        private readonly int _permisoId;

        public PermisoAttribute(int permisoId)
        {
            _permisoId = permisoId;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var identity =
                HttpContext.Current.User.Identity as ClaimsIdentity;

            if (identity == null || !identity.IsAuthenticated)
                return false;

            var permisos =
                identity.Claims
                .Where(c => c.Type == "Permiso")
                .Select(c => int.Parse(c.Value));

            return permisos.Contains(_permisoId);
        }
    }
}