using AspNetServer.Hubs;
using Microsoft.AspNet.SignalR;

public static class EmpleadoNotifier
{
    public static void Insertado(int id)
    {
        Notify("Insertado", id);
    }

    public static void Actualizado(int id)
    {
        Notify("Actualizado", id);
    }

    public static void Eliminado(int id)
    {
        Notify("Eliminado", id);
    }

    private static void Notify(
        string accion,
        int id)
    {
        var context =
            GlobalHost.ConnectionManager
            .GetHubContext<EmpleadosHub>();

        context.Clients.All.empleadoActualizado(
            accion,
            id);
    }
}