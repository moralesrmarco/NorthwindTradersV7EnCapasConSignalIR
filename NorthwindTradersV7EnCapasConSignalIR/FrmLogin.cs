using BLL;
using Entities;
using System;
using System.Configuration;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmLogin : Form
    {
        public Usuario UsuarioLogueado { get; private set; }
        bool _imagenMostrada = true;
        byte numeroIntentos = 0;

        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private readonly UsuarioBLL _usuarioBLL;

        public FrmLogin()
        {
            InitializeComponent();
            this.Text = Utils.nwtr;
            _usuarioBLL = new UsuarioBLL(_connectionString);
            // Al presionar Enter se ejecuta btnEntrar_Click
            this.AcceptButton = btnEntrar;
        }

        private void btnEntrar_Click(object sender, EventArgs e)
        {
            try
            {
                UsuarioLogueado = new Usuario
                {
                    User = txtUsuario.Text.Trim(),
                    Password = Utils.ComputeSha256Hash(txtPwd.Text.Trim())
                };
                UsuarioLogueado = _usuarioBLL.ValidarUsuario(UsuarioLogueado);
                if (UsuarioLogueado.Id > 0)
                {
                    this.Close();
                    return;
                }
                numeroIntentos++;
                if (numeroIntentos >= 3)
                {
                    U.NotificacionError("Demasiados intentos fallidos.\n\nLa aplicación se cerrará.");
                    Application.Exit();
                    return;
                }
                U.NotificacionError("Error de autenticación.\n\nUsuario o contraseña incorrectos.");
                txtPwd.Clear();
                txtPwd.Focus();
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        private void btnTogglePwd_Click(object sender, EventArgs e)
        {
            _imagenMostrada = !_imagenMostrada;
            txtPwd.UseSystemPasswordChar = !txtPwd.UseSystemPasswordChar;
            btnTogglePwd.Image = _imagenMostrada ? Properties.Resources.mostrarCh : Properties.Resources.ocultarCh;
        }
    }
}
