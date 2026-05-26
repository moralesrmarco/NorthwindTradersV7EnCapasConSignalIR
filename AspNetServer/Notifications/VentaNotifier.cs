using AspNetServer.Hubs;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

public static class VentaNotifier
{
    public static Task Insertado(int id)
    {
        return Notify("Insertado", id);
    }

    public static Task Actualizado(int id)
    {
        return Notify("Actualizado", id);
    }

    public static Task Eliminado(int id)
    {
        return Notify("Eliminado", id);
    }

    private static Task Notify(
        string accion,
        int id)
    {
        var context =
            GlobalHost.ConnectionManager
            .GetHubContext<VentasHub>();

        context.Clients.All.ventaActualizada(
            accion,
            id);

        return Task.CompletedTask;
    }
}