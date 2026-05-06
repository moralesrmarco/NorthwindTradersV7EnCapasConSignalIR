using Microsoft.AspNet.SignalR;

namespace AspNetServer.Hubs
{
    public class ClientesHub : Hub
    {
        public void NotificarCambio(string accion, int clienteId)
        {
            // Notifica a todos los clientes conectados
            Clients.All.clienteActualizado(accion, clienteId);
        }
    }
}