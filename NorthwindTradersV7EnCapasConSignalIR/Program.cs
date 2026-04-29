using Entities;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Detectar la pantalla en la que está el cursor
            Point posicionCursor = Cursor.Position;
            Screen pantallaDestino = Screen.FromPoint(posicionCursor);

            Usuario usuario = null;
            // Mostrar el formulario de login en la pantalla seleccionada
            //using (FrmLogin loginForm = new FrmLogin())
            //{
            //    loginForm.StartPosition = FormStartPosition.Manual;
            //    // Centrar el formulario en la pantalla destino
            //    var area = pantallaDestino.WorkingArea;
            //    loginForm.Location = new System.Drawing.Point(
            //        area.Left + (area.Width - loginForm.Width) / 2,
            //        area.Top + (area.Height - loginForm.Height) / 2
            //    );
            //    loginForm.ShowDialog();
            //    usuario = loginForm.Usuario;
            //    if (usuario.Id == 0)
            //    {
            //        return;
            //    }
            //}

            // Detectar la pantalla en la que está el cursor
            Point posicionCursor2 = Cursor.Position;
            Screen pantallaDestino2 = Screen.FromPoint(posicionCursor2);

            // Instanciar el MDIPrincipal en la misma pantalla
            MDIPrincipal mdiPrincipal = new MDIPrincipal
            {
                //Usuario = usuario,
                Usuario = new Usuario { Id = 1, Paterno = "Morales", Materno = "Rodríguez", Nombres = "Marco Antonio", User = "mmorales" }, // Simulación de usuario autenticado
                StartPosition = FormStartPosition.Manual,
                Bounds = pantallaDestino2.WorkingArea
            };
            Application.Run(mdiPrincipal);
        }
    }
}
