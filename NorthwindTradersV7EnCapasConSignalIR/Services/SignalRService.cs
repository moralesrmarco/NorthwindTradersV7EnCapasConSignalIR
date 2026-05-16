using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NorthwindTradersV7EnCapasConSignalIR.Services
{
    public class SignalRService
    {
        private HubConnection _connection;
        private IHubProxy _hubProxy;
        private bool _reconectando = false;
        private bool _cerrandoManual = false;

        public async Task ConectarAsync()
        {
            try
            {
                // Evita crear múltiples conexiones
                if (_connection != null &&
                    _connection.State != ConnectionState.Disconnected)
                {
                    return;
                }

                var urlBase =
                    ConfigurationManager.AppSettings["UrlBaseSignalR"];

                var query =
                    new Dictionary<string, string>
                    {
                        { "access_token", SesionActual.AccessToken }
                    };

                _connection =
                    new HubConnection(urlBase, query);

                _hubProxy =
                    _connection.CreateHubProxy("EmpleadosHub");

                // Importante:
                // quitar antes de agregar para evitar duplicados
                _connection.Closed -= OnConnectionClosed;
                _connection.Closed += OnConnectionClosed;

                await _connection.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error al conectar con el servidor SignalR: " +
                    ex.Message, ex);
            }
        }

        public Task DesconectarAsync()
        {
            if (_connection != null)
            {
                _cerrandoManual = true;

                _connection.Closed -= OnConnectionClosed;

                if (_connection.State != ConnectionState.Disconnected)
                {
                    _connection.Stop();
                }

                _connection.Dispose();

                _connection = null;
                _hubProxy = null;

                _cerrandoManual = false;
            }

            return Task.CompletedTask;
        }

        private void OnConnectionClosed()
        {
            if (_cerrandoManual)
                return;

            _ = ConnectionClosedHandler();
        }

        private async Task ConnectionClosedHandler()
        {
            if (_reconectando)
                return;

            _reconectando = true;

            try
            {
                bool refreshed =
                    await SesionActual.RefreshAccessTokenAsync(
                        SesionActual.UrlBaseSignalR);

                if (!refreshed)
                {
                    AppShutdownService.LogoutAndClose();
                    return;
                }

                int intentos = 0;

                while (intentos < 5)
                {
                    try
                    {
                        await ReconectarAsync();

                        return;
                    }
                    catch
                    {
                        intentos++;

                        await Task.Delay(3000);
                    }
                }

                MessageBox.Show(
                    "No fue posible reconectar con el servidor.");
            }
            finally
            {
                _reconectando = false;
            }
        }

        public async Task<bool> ReconectarAsync()
        {
            try
            {
                await DesconectarAsync();

                await ConectarAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}