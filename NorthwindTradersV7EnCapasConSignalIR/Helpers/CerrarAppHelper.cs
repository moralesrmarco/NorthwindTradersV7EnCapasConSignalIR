using System.Linq;
using System.Windows.Forms;

namespace NorthwindTradersV7EnCapasConSignalIR.Helpers
{
    public static class CerrarAppHelper
    {
        public static void CerrarApp()
        {
            var formulariosAbiertos = Application.OpenForms.Cast<Form>().ToList();
            foreach (var form in formulariosAbiertos)
            {
                if (form is FrmEmpleadosCrud empleadosForm)
                {
                    empleadosForm.FormClosing -= empleadosForm.FrmEmpleadosCrud_FormClosing;
                    form.Close();
                }
            }
            Application.Restart();
        }
    }
}
