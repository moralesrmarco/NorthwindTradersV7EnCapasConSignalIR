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

        // aqui deben ir propiedades específicas para cada hub, para evitar tener que usar strings en el código cliente
        public IHubProxy EmpleadosHub { get; private set; }
        public IHubProxy ClientesHub { get; private set; }

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
                if (_connection != null &&
                    _connection.State != ConnectionState.Disconnected)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(_urlBase))
                {
                    throw new InvalidOperationException(
                        "Debe configurar la URL antes de conectar.");
                }

                _connection =
                    new HubConnection(_urlBase);

                _connection.Closed -= OnConnectionClosed;
                _connection.Closed += OnConnectionClosed;

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

                if (_connection.Headers.ContainsKey("Authorization"))
                {
                    _connection.Headers.Remove("Authorization");
                }

                // JWT / Bearer token
                _connection.Headers.Add(
                    "Authorization",
                    $"Bearer {_accessToken}");

                // MUY importante:
                // CreateHubProxy(...)
                // siempre antes de:
                // Start()
                EmpleadosHub =
                    _connection.CreateHubProxy("EmpleadosHub");
                ClientesHub =
                    _connection.CreateHubProxy("ClientesHub");

                // registrar nuevamente las suscripciones
                foreach (var sub in _subscriptions)
                {
                    sub();
                }

                await _connection.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error al conectar con el servidor SignalR: " +
                    ex.Message, ex);
            }
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
                EmpleadosHub = null;

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
            if (!_subscriptions.Contains(accion))
            {
                _subscriptions.Add(accion);
            }
        }
        public void Configurar(string urlBase, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(urlBase))
            {
                throw new ArgumentException(
                    "La URL base es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException(
                    "El access token es obligatorio.");
            }

            _urlBase = urlBase;
            _accessToken = accessToken;
        }

        public async Task ActualizarTokenYReconectarAsync(string nuevoToken)
        {
            _accessToken = nuevoToken;

            await ReconectarAsync();
        }

        public void DesregistrarSuscripcion(Action accion)
        {
            if (_subscriptions.Contains(accion))
            {
                _subscriptions.Remove(accion);
            }
        }
    }
}