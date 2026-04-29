using Microsoft.AspNet.SignalR;

namespace AspNetServer.Hubs
{
    public class ChatHub : Hub
    {
        public void Send(string name, string message)
        {
            // Envía el mensaje a todos los clientes conectados
            Clients.All.addMessage(name, message);
        }
    }
}
