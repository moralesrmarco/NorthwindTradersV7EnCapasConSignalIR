using Entities;
using NorthwindTradersV7EnCapasConSignalIR.Services;
using System;
using System.Configuration;
using System.Net.Http;
using System.Windows.Forms;
using Utilities;
using Infrastructure.Services;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmLogin : Form
    {
        public Usuario UsuarioLogueado { get; private set; }
        bool _imagenMostrada = true;
        byte numeroIntentos = 0;

        public FrmLogin()
        {
            InitializeComponent();
            this.Text = Utils.nwtr;
            // Al presionar Enter se ejecuta btnEntrar_Click
            this.AcceptButton = btnEntrar;
        }

        private async void btnEntrar_Click(object sender, EventArgs e)
        {
            try
            {
                btnTogglePwd.Enabled = false;
                btnEntrar.Enabled = false;

                var client = HttpClientProvider.Client;

                var response =
                    await client.PostAsJsonAsync(
                        "api/auth/login",
                        new Usuario()
                        {
                            User = txtUsuario.Text.Trim(),
                            Password = Utils.ComputeSha256Hash(
                                txtPwd.Text.Trim())
                        });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<dynamic>();

                    SesionActual.AccessToken = (string)result.AccessToken;
                    SesionActual.RefreshToken = (string)result.RefreshToken;
                    SesionActual.Usuario = (string)result.Usuario.User;

                    try
                    {
                        // =========================
                        // CONFIGURAR SIGNALR
                        // =========================
                        await SignalRService.Instance.DesconectarAsync();

                        SignalRService.Instance.Configurar(
                            ConfigurationManager
                                .AppSettings["UrlBaseSignalR"],
                            SesionActual.AccessToken);

                        await SignalRService.Instance.ConectarAsync();
                    }
                    catch (Exception)
                    {
                        U.NotificacionWarning("No se pudo conectar al servicio de notificaciones en tiempo real.\n\n");
                        this.Close();
                        return;
                    }

                    // Asignar datos del usuario autenticado
                    UsuarioLogueado = new Usuario
                    {
                        Id = (int)result.Usuario.Id,
                        User = (string)result.Usuario.User,
                        Paterno = (string)result.Usuario.Paterno,
                        Materno = (string)result.Usuario.Materno,
                        Nombres = (string)result.Usuario.Nombres
                    };

                    this.DialogResult = DialogResult.OK;

                    this.Close();
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
            finally
            {
                btnTogglePwd.Enabled = true;
                btnEntrar.Enabled = true;
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
