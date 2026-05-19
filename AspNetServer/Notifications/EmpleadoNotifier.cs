using AspNetServer.Hubs;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

public static class EmpleadoNotifier
{
    public static async Task Insertado(int id)
    {
        await Notify("Insertado", id);
    }

    public static async Task Actualizado(int id)
    {
        await Notify("Actualizado", id);
    }

    public static async Task Eliminado(int id)
    {
        await Notify("Eliminado", id);
    }

    private static Task Notify(
        string accion,
        int id)
    {
        var context =
            GlobalHost.ConnectionManager
            .GetHubContext<EmpleadosHub>();

        context.Clients.All.empleadoActualizado(
            accion,
            id);

        return Task.CompletedTask;
    }
}