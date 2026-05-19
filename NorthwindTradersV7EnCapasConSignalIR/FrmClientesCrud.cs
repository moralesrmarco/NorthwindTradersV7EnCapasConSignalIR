using BLL;
using BLL.Services;
using Entities;
using Infrastructure.Services;
using Microsoft.AspNet.SignalR.Client;
using NorthwindTradersV7EnCapasConSignalIR.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmClientesCrud : Form
    {
        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private ClienteBLL _clienteBLL;
        private ClienteService _clienteService;

        private bool EjecutarConfDgv = true;
        internal Dictionary<string, object> valoresOriginales;
        private bool realizandoBusqueda = false;

        private IHubProxy _hubClientes;
        private IDisposable _subClienteActualizado;

        public FrmClientesCrud()
        {
            InitializeComponent();
            _clienteBLL = new ClienteBLL(_connectionString);
            _clienteService = new ClienteService(_connectionString);
        }

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmClientesCrud_FormClosed(object sender, FormClosedEventArgs e) => MDIPrincipal.ActualizarBarraDeEstado();


        internal void FrmClientesCrud_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppShutdownService.CerrandoPorLogout)
                return;

            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
            {
                if (U.NotificacionQuestion(Utils.preguntaCerrar) == DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void tabcOperacion_DrawItem(object sender, DrawItemEventArgs e) => Utils.DibujarPestañas(sender as TabControl, e);

        private void FrmClientesCrud_Load(object sender, EventArgs e)
        {
            tabcOperacion.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabcOperacion.DrawItem += tabcOperacion_DrawItem;
            DeshabilitarControles();
            LlenarCboPais();
            Utils.ConfDgv(dgv);
            LlenarDgv(false);
            CargarValoresOriginales();

            // Aquí agregas la conexión SignalR
            _hubClientes = SignalRService.Instance
                .ObtenerHubProxy("ClientesHub");

            _subClienteActualizado =
                _hubClientes.On<string, string>(
                    "clienteActualizado",
                    OnClienteActualizado);
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            try
            {
                _subClienteActualizado?.Dispose();
                _subClienteActualizado = null;
                _hubClientes = null;
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }

        private void OnClienteActualizado(string accion, string clienteId)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                BeginInvoke(new Action(() =>
                {
                    if (realizandoBusqueda)
                        return; // No refrescar si estás realizando búsqueda

                    LlenarDgv(false);

                    if (tabcOperacion.SelectedTab == tbpListar ||
                        tabcOperacion.SelectedTab == tbpRegistrar ||
                        tabcOperacion.SelectedTab == tbpEliminar)
                        return;

                    if (!string.IsNullOrWhiteSpace(txtId.Text))
                    {
                        string clienteActual =
                        txtId.Text;

                        if (clienteActual == clienteId)
                        {
                            CargarCliente(clienteId);
                        }
                    }
                }));
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private bool CargarCliente(string clienteId)
        {
            try
            {
                Cliente cliente = _clienteBLL.ObtenerClientePorId(clienteId);
                if (cliente == null)
                {
                    U.NotificacionWarning($"No se encontró el cliente con Id: {clienteId}." + Utils.erfep1);
                    BorrarDatosCliente();
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    CargarValoresOriginales();
                    return false;
                }
                txtId.Text = cliente.CustomerID;
                txtId.Tag = cliente.RowVersion;
                txtCompañia.Text = cliente.CompanyName;
                txtContacto.Text = cliente.ContactName;
                txtTitulo.Text = cliente.ContactTitle;
                txtDomicilio.Text = cliente.Address;
                txtCiudad.Text = cliente.City;
                txtRegion.Text = cliente.Region;
                txtCodigoP.Text = cliente.PostalCode;
                cboPais.Text = cliente.Country;
                txtTelefono.Text = cliente.Phone;
                txtFax.Text = cliente.Fax;
                return true;
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
                return false;
            }
        }

        private void DeshabilitarControles()
        {
            txtId.ReadOnly = txtCompañia.ReadOnly = txtContacto.ReadOnly = txtTitulo.ReadOnly = true;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = true;
            txtTelefono.ReadOnly = txtFax.ReadOnly = true;
            cboPais.Enabled = false;
            btnOperacion.Visible = false;
        }

        private void HabilitarControles()
        {
            txtId.ReadOnly = txtCompañia.ReadOnly = txtContacto.ReadOnly = txtTitulo.ReadOnly = false;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = false;
            txtTelefono.ReadOnly = txtFax.ReadOnly = false;
            cboPais.Enabled = true;
            btnOperacion.Visible = true;
        }

        void LlenarCboPais()
        {
            try
            {
                
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                string clienteId = "";
                string selectedValueCboPais = cboPais.SelectedValue?.ToString();
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    tieneId = true;
                    clienteId = txtId.Text;
                }
                DataTable paises = _clienteBLL.ObtenerClientesPaisesCbo();
                cboBPais.DataSource = paises;
                cboBPais.ValueMember = "Id";
                cboBPais.DisplayMember = "Pais";
                cboBPais.SelectedIndex = 0;

                cboPais.DataSource = paises.Copy();
                cboPais.ValueMember = "Id";
                cboPais.DisplayMember = "Pais";
                cboPais.SelectedIndex = 0;

                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    if (tieneId)
                    {
                        var pais = _clienteService.ObtenerClientePais(clienteId);
                        if (pais != null)
                            cboPais.SelectedValue = pais;
                        else
                        {
                            // esto es para aceptar texto libre 
                            if (!string.IsNullOrWhiteSpace(selectedValueCboPais))
                            {
                                cboPais.Text = selectedValueCboPais;
                                int idx = cboPais.FindStringExact(selectedValueCboPais);
                                if (idx >= 0)
                                    cboPais.SelectedIndex = idx; // coincide con un ítem
                                else
                                {
                                    cboPais.SelectedIndex = -1; // texto libre
                                    cboPais.Text = selectedValueCboPais; // mantener el texto libre
                                }
                            }
                            else
                            {
                                cboPais.SelectedIndex = 0;
                            }
                        }
                    }
                }
                if ((tabcOperacion.SelectedTab == tbpRegistrar || tabcOperacion.SelectedTab == tbpEliminar || tabcOperacion.SelectedTab == tbpListar) && !string.IsNullOrWhiteSpace(selectedValueCboPais))
                {
                    // esto es para aceptar texto libre 
                    cboPais.Text = selectedValueCboPais;
                    int idx = cboPais.FindStringExact(selectedValueCboPais);
                    if (idx >= 0)
                        cboPais.SelectedIndex = idx; // coincide con un ítem
                    else
                    {
                        cboPais.SelectedIndex = -1; // texto libre
                        cboPais.Text = selectedValueCboPais;
                    }
                }
                CargarValoresOriginales();
                MDIPrincipal.ActualizarBarraDeEstado();
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        void LlenarDgv(bool selectorRealizaBusqueda)
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                Cliente criterios = new Cliente()
                {
                    CustomerID = txtBId.Text,
                    CompanyName = txtBCompañia.Text,
                    ContactName = txtBContacto.Text,
                    Address = txtBDomicilio.Text,
                    City = txtBCiudad.Text,
                    Region = txtBRegion.Text,
                    PostalCode = txtBCodigoP.Text,
                    Country = cboBPais.SelectedValue.ToString(),
                    Phone = txtBTelefono.Text,
                    Fax = txtBFax.Text
                };
                var resultado = _clienteBLL.ObtenerClientes(selectorRealizaBusqueda, criterios);
                dgv.DataSource = resultado.clientes;
                if (EjecutarConfDgv)
                {
                    ConfDgv();
                    EjecutarConfDgv = false;
                }
                LlenarCboPais();
                MDIPrincipal.ActualizarBarraDeEstado(resultado.mensajeEstado);
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        private void ConfDgv()
        {
            dgv.Columns["RowVersion"].Visible = false;

            dgv.Columns["CustomerID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["ContactTitle"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["City"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Region"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["PostalCode"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Country"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Phone"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Fax"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            dgv.Columns["City"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Region"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["PostalCode"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Country"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["CustomerID"].HeaderText = "Id";
            dgv.Columns["CompanyName"].HeaderText = "Nombre de compañía";
            dgv.Columns["ContactName"].HeaderText = "Nombre del contacto";
            dgv.Columns["ContactTitle"].HeaderText = "Título del contacto";
            dgv.Columns["Address"].HeaderText = "Domicilio";
            dgv.Columns["City"].HeaderText = "Ciudad";
            dgv.Columns["Region"].HeaderText = "Región";
            dgv.Columns["PostalCode"].HeaderText = "Código postal";
            dgv.Columns["Country"].HeaderText = "País";
            dgv.Columns["Phone"].HeaderText = "Teléfono";
            dgv.Columns["Fax"].HeaderText = "Fax";
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BorrarMensajesError();
            BorrarDatosCliente();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(true);
            CargarValoresOriginales();
            realizandoBusqueda = true;
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            BorrarMensajesError();
            BorrarDatosBusqueda();
            BorrarDatosCliente();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(false);
            CargarValoresOriginales();
            realizandoBusqueda = false;
        }

        void BorrarMensajesError() => errorProvider1.Clear();

        void BorrarDatosCliente()
        {
            txtId.Text = txtCompañia.Text = txtContacto.Text = txtDomicilio.Text = txtCiudad.Text = "";
            txtRegion.Text = txtCodigoP.Text = txtTelefono.Text = txtFax.Text = txtTitulo.Text = "";
            cboPais.SelectedIndex = 0;
        }

        void BorrarDatosBusqueda()
        {
            txtBId.Text = txtBCompañia.Text = txtBContacto.Text = txtBDomicilio.Text = txtBCiudad.Text = "";
            txtBRegion.Text = txtBCodigoP.Text = txtBTelefono.Text = txtBFax.Text = "";
            cboBPais.SelectedIndex = 0;
        }

        // txtBId tambien se engancha al mismo evento
        private void txtId_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo letras y la tecla de retroceso
            if (!char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Bloquea el carácter
            }
        }

        // txtBId tambien se engancha al mismo evento
        // se usa TextChanged para casos como pegar texto
        private void txtId_TextChanged(object sender, EventArgs e)
        {
            // Castear el objeto que disparó el evento
            TextBox tb = sender as TextBox;
            if (tb == null) return; // seguridad
            if (tb == txtId)
                // Lógica para txtId
                tb.Text = Regex.Replace(tb.Text, @"[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]", "");
            else if (tb == txtBId)
                // Lógica para txtBid (similar pero aplicado a txtBid)
                tb.Text = Regex.Replace(tb.Text, @"[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s]", "");
        }

        // txtBId tambien se engancha al mismo evento
        private void txtId_KeyDown(object sender, KeyEventArgs e)
        {
            // Permitir teclas especiales: Suprimir, Inicio, Fin, Flechas
            if (e.KeyCode == Keys.Delete ||
                e.KeyCode == Keys.Home ||
                e.KeyCode == Keys.End ||
                e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.Right)
            {
                e.SuppressKeyPress = false; // Se permite
            }
        }

        private bool ValidarControles()
        {
            bool valida = true;
            if (txtId.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtId, "Ingrese el Id del cliente");
            }
            if (txtCompañia.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtCompañia, "Ingrese el nombre de la compañía");
            }
            if (txtContacto.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtContacto, "Ingrese el nombre del contacto");
            }
            if (txtTitulo.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtTitulo, "Ingrese el título del contacto");
            }
            if (txtDomicilio.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtDomicilio, "Ingrese el domicilio");
            }
            if (txtCiudad.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtCiudad, "Ingrese la ciudad");
            }
            if (string.IsNullOrWhiteSpace(cboPais.Text) || cboPais.SelectedIndex == 0)
            {
                valida = false;
                errorProvider1.SetError(cboPais, "Ingrese o seleccione el país");
            }
            if (txtTelefono.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtTelefono, "Ingrese el teléfono");
            }
            return valida;
        }

        private void tabcOperacion_Selected(object sender, TabControlEventArgs e)
        {
            BorrarDatosCliente();
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                BorrarDatosBusqueda();
                HabilitarControles();
                txtId.Enabled = true;
                txtId.ReadOnly = false;
                btnOperacion.Text = "Registrar cliente";
                btnOperacion.Enabled = true;
            }
            else
            {
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                dgv.CellClick += new DataGridViewCellEventHandler(dgv_CellClick);
                DeshabilitarControles();
                btnOperacion.Enabled = false;
                if (tabcOperacion.SelectedTab == tbpListar)
                    btnOperacion.Visible = false;
                else if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    btnOperacion.Text = "Modificar cliente";
                    btnOperacion.Visible = true;
                }
                else if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Text = "Eliminar cliente";
                    btnOperacion.Visible = true;
                }
            }
            CargarValoresOriginales();
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 🔹 Cuando viene desde un click real del DataGridView
            if (e != null)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;
                DataGridViewRow dgvr = dgv.Rows[e.RowIndex];
                if (dgvr.Cells["CustomerID"].Value == null)
                    return;
                txtId.Text = dgvr.Cells["CustomerID"].Value.ToString();
            }
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
            {
                DeshabilitarControles();
                try
                {
                    string clienteId = txtId.Text;
                    if (!CargarCliente(clienteId))
                    {
                        ActualizaDgv();
                        return;
                    }
                } 
                catch (Exception ex)
                { 
                    U.MsgCatchOue(ex);
                }
                if (tabcOperacion.SelectedTab == tbpListar)
                {
                    btnOperacion.Visible = false;
                }
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    HabilitarControles();
                    txtId.Enabled = false;
                    btnOperacion.Enabled = true;
                }
                if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = true;
                }
            }
            CargarValoresOriginales();
        }

        private void dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // debe estar vinculado a la clase List<> a la cual esta vinculado el DataGridView.DataSource
            Utils.OrdenarPorColumna<Cliente>(dgv, e);
        }

        private async void btnOperacion_Click(object sender, EventArgs e)
        {
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                if (ValidarControles())
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.insertandoRegistro);
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    try
                    {
                        var cliente = new Cliente
                        {
                            CustomerID = txtId.Text.Trim(),
                            CompanyName = txtCompañia.Text.Trim(),
                            ContactName = txtContacto.Text.Trim(),
                            ContactTitle = txtTitulo.Text.Trim(),
                            Address = txtDomicilio.Text.Trim(),
                            City = txtCiudad.Text.Trim(),
                            Region = string.IsNullOrWhiteSpace(txtRegion.Text.Trim()) ? null : txtRegion.Text.Trim(),
                            PostalCode = string.IsNullOrWhiteSpace(txtCodigoP.Text.Trim()) ? null : txtCodigoP.Text.Trim(),
                            Country = cboPais.Text.Trim(),
                            Phone = txtTelefono.Text.Trim(),
                            Fax = string.IsNullOrWhiteSpace(txtFax.Text.Trim()) ? null : txtFax.Text.Trim()
                        };
                        var resultado = await ApiClienteService.InsertarAsync(cliente);
                        if (resultado.ok)
                        {
                            string idyNombreCompania = $"El cliente con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            U.NotificacionInformation(idyNombreCompania + Utils.srs);
                            MDIPrincipal.ActualizarBarraDeEstado();
                        }
                        else
                            U.NotificacionError(resultado.mensaje);
                    }
                    catch (Exception ex)
                    {
                        U.MsgCatchOue(ex);
                    }
                    HabilitarControles();
                    btnOperacion.Enabled = true;
                }
            }
            else if (tabcOperacion.SelectedTab == tbpModificar)
            {
                // Verificar si hubo cambios en el formulario
                if (!Utils.HayCambios(this, valoresOriginales))
                {
                    U.NotificacionWarning(Utils.ndc);
                    return; // Salir sin hacer UPDATE
                }
                if (ValidarControles())
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.modificandoRegistro);
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    try
                    {
                        var cliente = new Cliente()
                        {
                            CustomerID = txtId.Text,
                            CompanyName = txtCompañia.Text,
                            ContactName = txtContacto.Text,
                            ContactTitle = txtTitulo.Text,
                            Address = txtDomicilio.Text,
                            City = txtCiudad.Text,
                            Region = txtRegion.Text,
                            PostalCode = txtCodigoP.Text,
                            Country = cboPais.Text,
                            Phone = txtTelefono.Text,
                            Fax = txtFax.Text,
                            RowVersion = txtId.Tag as byte[]
                        };
                        var resultado = await ApiClienteService.ActualizarAsync(cliente);
                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se actualizó {(numRegs < 0 ? 0 : numRegs)} registro");
                            string idyNombreCompania = $"El cliente con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            if (numRegs > 0)
                            {
                                U.NotificacionInformation(idyNombreCompania + Utils.sms);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombreCompania + Utils.nfmfe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombreCompania + Utils.nfmfm);
                            else
                                U.NotificacionInformation(idyNombreCompania + Utils.nfmmd);
                        }
                        else
                            U.NotificacionError(resultado.mensaje);
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al modificar el cliente: " + ex.Message);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                }
            }
            else if (tabcOperacion.SelectedTab == tbpEliminar)
            {
                if (U.NotificacionQuestion($"[orange]¿Esta seguro de eliminar el cliente con Id: {txtId.Text} - Nombre de Compañía: {txtCompañia.Text}?") == DialogResult.Yes)
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.eliminandoRegistro);
                    btnOperacion.Enabled = false;
                    Cliente cliente = new Cliente()
                    {
                        CustomerID = txtId.Text,
                        RowVersion = txtId.Tag as byte[]
                    };
                    try
                    {
                        var resultado = await ApiClienteService.EliminarAsync(cliente.CustomerID, txtId.Tag as byte[]);
                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se eliminó {(numRegs < 0 ? 0 : numRegs)} registro");
                            string idyNombreCompania = $"El cliente con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            if (numRegs > 0)
                            {
                                U.NotificacionInformation(idyNombreCompania + Utils.ses);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombreCompania + Utils.nfefe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombreCompania + Utils.nfefm);
                            else
                                U.NotificacionError(idyNombreCompania + Utils.nfemd);
                        }
                        else
                        {
                            U.NotificacionError(resultado.mensaje);
                        }
                    }
                    catch (Exception ex)
                    {
                        U.MsgCatchOue(ex);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                }
            }
            BorrarDatosCliente();
            CargarValoresOriginales();
        }

        void ActualizaDgv() => btnLimpiar.PerformClick();

        private void CargarValoresOriginales()
        {
            // Captura inicial usando la utilidad
            valoresOriginales = Utils.CapturarValoresOriginales(this);
        }

        private void tabcOperacion_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
                if (U.NotificacionQuestion(Utils.preguntaCerrarPestaña) == DialogResult.No)
                    e.Cancel = true;
        }
    }
}
