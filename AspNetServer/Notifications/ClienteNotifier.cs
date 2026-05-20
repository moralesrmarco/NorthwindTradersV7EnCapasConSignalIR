using AspNetServer.Hubs;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace AspNetServer.Notifications
{
    public static class ClienteNotifier
    {
        public static async Task Insertado(string clienteId)
        {
            await Notify("Insertado", clienteId);
        }

        public static async Task Actualizado(string clienteId)
        {
            await Notify("Actualizado", clienteId);
        }

        public static async Task Eliminado(string clienteId)
        {
            await Notify("Eliminado", clienteId);
        }

        private static Task Notify(
            string accion,
            string clienteId)
        {
            var context =
                GlobalHost.ConnectionManager
                .GetHubContext<ClientesHub>();

            context.Clients.All.clienteActualizado(
                accion,
                clienteId);

            return Task.CompletedTask;
        }

    }
}