using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SignalRService
    {
        // =========================
        // SINGLETON
        // =========================
        private static readonly Lazy<SignalRService> _instance =
            new Lazy<SignalRService>(() => new SignalRService());

        public static SignalRService Instance =>
            _instance.Value;

        private SignalRService()
        {
        }

        // =========================
        // CAMPOS
        // =========================
        private HubConnection _connection;

        private readonly Dictionary<string, IHubProxy> _hubs =
            new Dictionary<string, IHubProxy>();
        private readonly List<Action> _subscriptions =
            new List<Action>();

        private bool _reconectando = false;
        private bool _cerrandoManual = false;

        private string _urlBase;
        private string _accessToken;

        // =========================
        // EVENTOS
        // =========================
        public event Action<string> EstadoConexion;
        public event Action<string> ErrorConexion;
        public event Action SolicitarLogout;

        // =========================
        // ESTADO
        // =========================
        public bool EstaConectado =>
            _connection != null &&
            _connection.State == ConnectionState.Connected;

        // =========================
        // CONECTAR
        // =========================
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

                var query =
                    new Dictionary<string, string>
                    {
                        { "access_token", _accessToken }
                    };

                _connection =
                    new HubConnection(_urlBase, query);

                // Importante:
                // quitar antes de agregar para evitar duplicados
                _connection.Closed -= OnConnectionClosed;
                _connection.Closed += OnConnectionClosed;

                await _connection.Start();

                foreach (var sub in _subscriptions)
                {
                    sub();
                }
                _connection.Reconnecting += () =>
                {
                    EstadoConexion?.Invoke("Reconectando...");
                };

                _connection.Reconnected += () =>
                {
                    EstadoConexion?.Invoke("Reconectado.");
                };

                _connection.Error += ex =>
                {
                    ErrorConexion?.Invoke(ex.Message);
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error al conectar con el servidor SignalR: " +
                    ex.Message, ex);
            }
        }

        // =========================
        // OBTENER HUB
        // =========================
        public IHubProxy ObtenerHubProxy(string hubName)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(
                    "SignalR no está conectado.");
            }

            if (_hubs.ContainsKey(hubName))
            {
                return _hubs[hubName];
            }

            var hub =
                _connection.CreateHubProxy(hubName);

            _hubs.Add(hubName, hub);

            return hub;
        }

        // =========================
        // DESCONECTAR
        // =========================
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

                _hubs.Clear();

                _cerrandoManual = false;
            }

            return Task.CompletedTask;
        }

        // =========================
        // CLOSED
        // =========================
        private void OnConnectionClosed()
        {
            if (_cerrandoManual)
                return;

            _ = ConnectionClosedHandler();
        }

        // =========================
        // RECONEXIÓN
        // =========================
        private Task ConnectionClosedHandler()
        {
            if (_reconectando)
                return Task.CompletedTask;

            _reconectando = true;

            try
            {
                SolicitarLogout?.Invoke();
                return Task.CompletedTask;
            }
            finally
            {
                _reconectando = false;
            }
        }

        // =========================
        // RECONECTAR
        // =========================
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

        public void RegistrarSuscripcion(Action accion)
        {
            _subscriptions.Add(accion);

            accion();
        }

        public void Configurar(string urlBase, string accessToken)
        {
            _urlBase = urlBase;
            _accessToken = accessToken;
        }

        public async Task ActualizarTokenYReconectarAsync(string nuevoToken)
        {
            _accessToken = nuevoToken;

            await ReconectarAsync();
        }
    }
}