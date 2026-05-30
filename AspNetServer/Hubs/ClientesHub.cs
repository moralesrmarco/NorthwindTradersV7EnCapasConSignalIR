using Microsoft.AspNet.SignalR;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspNetServer.Hubs
{
    [Authorize]
    public class ClientesHub : Hub
    {
        public override Task OnConnected()
        {
            var identity =
                Context.User.Identity as ClaimsIdentity;

            if (identity == null ||
                !identity.IsAuthenticated)
            {
                return base.OnConnected();
            }

            var permisos =
                identity.Claims
                .Where(c => c.Type == "Permiso")
                .Select(c =>
                {
                    int.TryParse(c.Value, out int p);
                    return p;
                });

            // Cambia el permiso según tu lógica
            if (!permisos.Contains(2))
            {
                return base.OnConnected();
            }

            try
            {
            }
            catch
            {
            }

            return base.OnConnected();
        }
    }
}