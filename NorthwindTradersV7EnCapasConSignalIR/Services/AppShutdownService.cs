using Infrastructure.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class AppShutdownService
    {
        private static bool _cerrando = false;

        public static bool CerrandoPorLogout { get; private set; }

        public static async Task LogoutAndClose()
        {
            if (_cerrando)
                return;

            _cerrando = true;
            CerrandoPorLogout = true;

            try
            {
                U.NotificacionInformation("Su sesión ha expirado.\n\nLa aplicación se cerrará.\n\nPor favor, inicie sesión nuevamente.");

                await SignalRService.Instance
                    .DesconectarAsync();

                var formularios = Application.OpenForms.Cast<Form>().ToList();

                foreach (var form in formularios)
                {
                    form.Close();
                }

                Application.Restart();
            }
            catch
            {
                Application.Exit();
            }
        }
    }
}
