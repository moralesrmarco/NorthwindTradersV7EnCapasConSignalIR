using Microsoft.AspNet.SignalR;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspNetServer.Hubs
{
    [Authorize]
    public class ProveedoresHub : Hub
    {
        public override Task OnConnected()
        {
            var identity =
                Context.User.Identity as ClaimsIdentity;

            if (identity == null ||
                !identity.IsAuthenticated)
            {
                throw new HubException("No autorizado");
            }

            var permisos =
                identity.Claims
                .Where(c => c.Type == "Permiso")
                .Select(c =>
                {
                    int.TryParse(c.Value, out int p);
                    return p;
                });

            if (!permisos.Contains(3))
            {
                throw new HubException("Sin permisos");
            }

            return base.OnConnected();
        }

        public void NotificarProveedorActualizado(
            string accion,
            int proveedorId)
        {
            Clients.All.proveedorActualizado(
                accion,
                proveedorId);
        }
    }
}