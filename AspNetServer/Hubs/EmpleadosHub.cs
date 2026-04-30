using Microsoft.AspNet.SignalR;

namespace AspNetServer.Hubs
{
    public class EmpleadosHub : Hub
    {
        public void NotificarCambio(string accion, int empleadoId)
        {
            // Notifica a todos los clientes conectados
            Clients.All.empleadoActualizado(accion, empleadoId);
        }
    }
}
