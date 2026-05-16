using System.Linq;
using System.Windows.Forms;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public static class AppShutdownService
    {
        public static void LogoutAndClose()
        {
            try
            {
                U.NotificacionInformation("Su sesión ha expirado.\n\nLa aplicación se cerrará.\n\nPor favor, inicie sesión nuevamente.");

                var formularios = Application.OpenForms.Cast<Form>().ToList();

                foreach (var form in formularios)
                {
                    if (form is FrmEmpleadosCrud empleadosForm)
                    {
                        empleadosForm.FormClosing -= empleadosForm.FrmEmpleadosCrud_FormClosing;
                    }

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
