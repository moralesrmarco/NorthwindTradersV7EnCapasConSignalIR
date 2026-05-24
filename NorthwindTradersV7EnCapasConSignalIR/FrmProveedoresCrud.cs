using BLL;
using BLL.Services;
using Entities;
using Entities.DTOs;
using Infrastructure.Services;
using Microsoft.AspNet.SignalR.Client;
using NorthwindTradersV7EnCapasConSignalIR.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmProveedoresCrud : Form
    {
        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        ProveedorBLL _proveedorBLL;
        ProveedorService _proveedorService;
        private bool EjecutarConfDgv = true;
        internal Dictionary<string, object> valoresOriginales;
        private bool _realizandoBusqueda = false;
        private bool _procesandoSignalR = false;

        private IDisposable _proveedoresSubscription;

        public FrmProveedoresCrud()
        {
            InitializeComponent();
            _proveedorBLL = new ProveedorBLL(_connectionString);
            _proveedorService = new ProveedorService(_connectionString);
        }

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmProveedoresCrud_FormClosed(object sender, FormClosedEventArgs e) => MDIPrincipal.ActualizarBarraDeEstado();

        internal void FrmProveedoresCrud_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppShutdownService.CerrandoPorLogout)
                return;

            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
                if (U.NotificacionQuestion(Utils.preguntaCerrar) == DialogResult.No)
                    e.Cancel = true;
        }

        private void tabcOperacion_DrawItem(object sender, DrawItemEventArgs e) => Utils.DibujarPestañas(sender as TabControl, e);

        private void FrmProveedoresCrud_Load(object sender, EventArgs e)
        {
            tabcOperacion.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabcOperacion.DrawItem += tabcOperacion_DrawItem;
            DeshabilitarControles();
            LlenarCboPais();
            Utils.ConfDgv(Dgv);
            LlenarDgv(false);
            CargarValoresOriginales();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Action registrarEventos = () =>
            {
                _proveedoresSubscription?.Dispose();

                _proveedoresSubscription =
                    SignalRService.Instance.ProveedoresHub
                    .On<string, int>(
                        "proveedorActualizado",
                        ProveedorActualizadoHandler);
            };

            registrarEventos();

            SignalRService.Instance
                .RegistrarSuscripcion(registrarEventos);
        }

        private void ProveedorActualizadoHandler(string accion, int proveedorId)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                        ProveedorActualizadoHandler(accion, proveedorId)));
                    return;
                }

                if (_realizandoBusqueda)
                    return;

                if (_procesandoSignalR)
                    return;

                _procesandoSignalR = true;

                // 🔥 1. SI ES DELETE → SOLO REFRESCAR LISTA Y SALIR
                if (accion == "DELETE")
                {
                    LlenarDgv(false); // SOLO lista
                    _procesandoSignalR = false;
                    return;
                }
                // 🔥 2. PARA INSERT/UPDATE
                LlenarDgv(false);
                if (tabcOperacion.SelectedTab == tbpRegistrar ||
                    tabcOperacion.SelectedTab == tbpEliminar)
                {
                    _procesandoSignalR = false;
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtId.Text) &&
                    int.TryParse(txtId.Text, out int proveedorActual))
                {
                    if (proveedorActual == proveedorId)
                    {
                        CargarProveedor(proveedorId);
                    }
                }
            }
            finally
            {
                _procesandoSignalR = false;
            }
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            try
            {
                _proveedoresSubscription?.Dispose();
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }

        private bool CargarProveedor(int proveedorId)
        {
            try
            {
                Proveedor proveedor = _proveedorBLL.ObtenerProveedorPorId(proveedorId);
                if (proveedor == null)
                {
                    U.NotificacionWarning($"No se encontró el proveedor con Id: {proveedorId}." + Utils.erfep1);
                    BorrarDatosProveedor();
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    CargarValoresOriginales();
                    return false;
                }
                txtId.Text = proveedor.SupplierID.ToString();
                txtId.Tag = proveedor.RowVersion;
                txtCompañia.Text = proveedor.CompanyName;
                txtContacto.Text = proveedor.ContactName;
                txtTitulo.Text = proveedor.ContactTitle;
                txtDomicilio.Text = proveedor.Address;
                txtCiudad.Text = proveedor.City;
                txtRegion.Text = proveedor.Region;
                txtCodigoP.Text = proveedor.PostalCode;
                cboPais.Text = proveedor.Country;
                txtTelefono.Text = proveedor.Phone;
                txtFax.Text = proveedor.Fax;
                CargarValoresOriginales();
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
            txtCompañia.ReadOnly = txtContacto.ReadOnly = txtTitulo.ReadOnly = true;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = true;
            txtTelefono.ReadOnly = txtFax.ReadOnly = true;
            cboPais.Enabled = false;
        }

        private void HabilitarControles()
        {
            txtCompañia.ReadOnly = txtContacto.ReadOnly = txtTitulo.ReadOnly = false;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = false;
            txtTelefono.ReadOnly = txtFax.ReadOnly = false;
            cboPais.Enabled = true;
        }

        void LlenarCboPais()
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                int proveedorId = 0;
                string selectedValueCboPais = cboPais.Text;
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    proveedorId = 0;
                    tieneId = int.TryParse(txtId.Text, out proveedorId);
                }
                var paises = _proveedorBLL.ObtenerProveedorPaisesCbo();
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
                    // 🔹 Restaurar desde BD
                    if (tieneId)
                    {
                        var pais = _proveedorService.ObtenerProveedorPais(proveedorId);
                        // paises es una colección, pero solo debe haber uno o ninguno, así que buscamos el primero que coincida
                        if (!string.IsNullOrWhiteSpace(pais) &&
                            paises.AsEnumerable().Any(p => p["pais"]
                                .ToString()
                                .Equals(pais, StringComparison.OrdinalIgnoreCase)))
                        {
                            cboPais.Text = pais;
                        }
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
                if ((tabcOperacion.SelectedTab == tbpRegistrar || tabcOperacion.SelectedTab == tbpEliminar || tabcOperacion.SelectedTab == tbpConsultar) && !string.IsNullOrWhiteSpace(selectedValueCboPais))
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
                DtoProveedoresBuscar criterios = new DtoProveedoresBuscar
                {
                    IdIni = string.IsNullOrWhiteSpace(txtBIdIni.Text) ? 0 : int.Parse(txtBIdIni.Text),
                    IdFin = string.IsNullOrWhiteSpace(txtBIdFin.Text) ? 0 : int.Parse(txtBIdFin.Text),
                    CompanyName = txtBCompañia.Text.Trim(),
                    ContactName = txtBContacto.Text.Trim(),
                    Address = txtBDomicilio.Text.Trim(),
                    City = txtBCiudad.Text.Trim(),
                    Region = txtBRegion.Text.Trim(),
                    PostalCode = txtBCodigoP.Text.Trim(),
                    Country = cboBPais.SelectedValue.ToString(),
                    Phone = txtBTelefono.Text.Trim(),
                    Fax = txtBFax.Text.Trim()
                };
                var proveedores = _proveedorBLL.ObtenerProveedores(selectorRealizaBusqueda, criterios, false);
                Dgv.DataSource = proveedores;
                if (EjecutarConfDgv)
                {
                    ConfDgv();
                    EjecutarConfDgv = false;
                }
                LlenarCboPais();
                if (selectorRealizaBusqueda)
                    MDIPrincipal.ActualizarBarraDeEstado($"Se encontraron {Dgv.RowCount} registro(s)");
                else
                    MDIPrincipal.ActualizarBarraDeEstado($"Se muestran los últimos {Dgv.RowCount} proveedor(es) registrado(s)");
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        private void ConfDgv()
        {
            Dgv.Columns["RowVersion"].Visible = false;

            Dgv.Columns["SupplierID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["ContactTitle"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["City"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["Region"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["PostalCode"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["Country"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["Phone"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["Fax"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            Dgv.Columns["City"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["Region"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["PostalCode"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["Country"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            Dgv.Columns["SupplierID"].HeaderText = "Id";
            Dgv.Columns["CompanyName"].HeaderText = "Nombre de compañía";
            Dgv.Columns["ContactName"].HeaderText = "Nombre de contacto";
            Dgv.Columns["ContactTitle"].HeaderText = "Título de contacto";
            Dgv.Columns["Address"].HeaderText = "Domicilio";
            Dgv.Columns["City"].HeaderText = "Ciudad";
            Dgv.Columns["Region"].HeaderText = "Región";
            Dgv.Columns["PostalCode"].HeaderText = "Código postal";
            Dgv.Columns["Country"].HeaderText = "País";
            Dgv.Columns["Phone"].HeaderText = "Teléfono";
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BorrarDatosProveedor();
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(true);
            CargarValoresOriginales();
            _realizandoBusqueda = true;
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            BorrarMensajesError();
            BorrarDatosBusqueda();
            BorrarDatosProveedor();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(false);
            CargarValoresOriginales();
            _realizandoBusqueda = false;
        }

        void BorrarMensajesError() => errorProvider1.Clear();

        private void BorrarDatosBusqueda()
        {
            txtBIdIni.Text = txtBIdFin.Text = "";
            txtBCompañia.Text = txtContacto.Text = txtDomicilio.Text = txtCiudad.Text = txtRegion.Text = txtCodigoP.Text = "";
            cboBPais.SelectedIndex = 0;
            txtBTelefono.Text = txtBFax.Text = "";
        }

        private void BorrarDatosProveedor()
        {
            txtId.Text = txtCompañia.Text = txtContacto.Text = txtTitulo.Text = "";
            txtDomicilio.Text = txtCiudad.Text = txtRegion.Text = txtCodigoP.Text = "";
            txtTelefono.Text = txtFax.Text = "";
            cboPais.Text = null;
            cboPais.SelectedIndex = 0;
        }

        void txtBId_KeyPress(object sender, KeyPressEventArgs e) => Utils.ValidarDigitosSinPunto(sender, e);

        private void txtBId_Enter(object sender, EventArgs e) => ((TextBox)sender).SelectAll();

        void txtBId_Leave(object sender, EventArgs e)
        {
            // Castear el objeto que disparó el evento
            TextBox tb = sender as TextBox;
            if (tb == null) return; // seguridad
            if (tb == txtBIdIni)
                Utils.ValidaTxtBIdIni(txtBIdIni, txtBIdFin);
            else if (tb == txtBIdFin)
                Utils.ValidaTxtBIdFin(txtBIdIni, txtBIdFin);
        }

        private bool ValidarControles()
        {
            bool valida = true;
            if (txtCompañia.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtCompañia, "Ingrese el nombre de la compañía");
            }
            if (txtContacto.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtContacto, "Ingrese el nombre de contacto");
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
            if (cboPais.Text.Trim() == "" || cboPais.SelectedIndex == 0)
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

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 🔹 Cuando viene desde un click real del DataGridView
            if (e != null)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;
                DataGridViewRow dgvr = Dgv.Rows[e.RowIndex];
                if (dgvr.Cells["SupplierID"].Value == null)
                    return;
                txtId.Text = dgvr.Cells["SupplierID"].Value.ToString();
            }
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
            {
                DeshabilitarControles();
                //DataGridViewRow dgvr = Dgv.CurrentRow;
                //txtId.Text = dgvr.Cells["SupplierID"].Value.ToString();
                //Proveedor proveedor = new Proveedor();
                try
                {
                    int proveedorId = Convert.ToInt32(txtId.Text);
                    if (!CargarProveedor(proveedorId))
                    {
                        ActualizaDgv();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    U.MsgCatchOue(ex);
                }
                //if (tabcOperacion.SelectedTab == tbpConsultar)
                //{
                //    btnOperacion.Visible = true;
                //    btnOperacion.Enabled = true;
                //}
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    HabilitarControles();
                    btnOperacion.Enabled = true;
                }
                if (tabcOperacion.SelectedTab == tbpEliminar)
                    btnOperacion.Enabled = true;
            }
            CargarValoresOriginales();
        }

        private void Dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // debe estar vinculado a la clase List<> a la cual esta vinculado el DataGridView.DataSource
            Utils.OrdenarPorColumna<Proveedor>(Dgv, e);
        }

        void ActualizaDgv() => btnLimpiar.PerformClick();

        private void tabcOperacion_Selected(object sender, TabControlEventArgs e)
        {
            BorrarMensajesError();
            BorrarDatosProveedor();
            if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                Dgv.CellClick -= new DataGridViewCellEventHandler(Dgv_CellClick);
                Dgv.CellClick -= new DataGridViewCellEventHandler(Dgv_CellClick);
                BorrarDatosBusqueda();
                HabilitarControles();
                btnOperacion.Text = "Registrar proveedor";
                btnOperacion.Visible = true;
                btnOperacion.Enabled = true;
            }
            else
            {
                Dgv.CellClick -= new DataGridViewCellEventHandler(Dgv_CellClick);
                Dgv.CellClick += new DataGridViewCellEventHandler(Dgv_CellClick);
                DeshabilitarControles();
                btnOperacion.Enabled = false;
                if (tabcOperacion.SelectedTab == tbpConsultar)
                    btnOperacion.Visible = false;
                else if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    btnOperacion.Text = "Modificar proveedor";
                    btnOperacion.Visible = true;
                }
                else if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Text = "Eliminar proveedor";
                    btnOperacion.Visible = true;
                }
            }
            CargarValoresOriginales();
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
                        var proveedor = new Proveedor
                        {
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
                        var resultado = await ApiProveedorService.InsertarAsync(proveedor);
                        if (resultado.ok)
                        {
                            txtId.Text = resultado.proveedor.SupplierID.ToString();
                            string idyNombreCompania = $"El proveedor con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            MDIPrincipal.ActualizarBarraDeEstado($"Se insertó 1 registro");
                            U.NotificacionInformation(idyNombreCompania +
                                Utils.srs);
                            //await SignalRService
                            //    .Instance
                            //    .ProveedoresHub
                            //    .Invoke(
                            //        "NotificarProveedorActualizado", "INSERT",
                            //        resultado.proveedor.SupplierID);
                            BorrarDatosProveedor();
                            CargarValoresOriginales();
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
                        var proveedor = new Proveedor
                        {
                            SupplierID = int.Parse(txtId.Text),
                            CompanyName = txtCompañia.Text.Trim(),
                            ContactName = txtContacto.Text.Trim(),
                            ContactTitle = txtTitulo.Text.Trim(),
                            Address = txtDomicilio.Text.Trim(),
                            City = txtCiudad.Text.Trim(),
                            Region = txtRegion.Text.Trim(),
                            PostalCode = txtCodigoP.Text.Trim(),
                            Country = cboPais.Text.Trim(),
                            Phone = txtTelefono.Text.Trim(),
                            Fax = txtFax.Text.Trim(),
                            RowVersion = txtId.Tag as byte[]
                        };
                        var resultado = await ApiProveedorService.ActualizarAsync(proveedor);
                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se actualizaron {(numRegs < 0 ? 0 : numRegs)} registro(s)");
                            string idyNombreCompania = $"El proveedor con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            if (numRegs > 0)
                            {
                                U.NotificacionInformation(idyNombreCompania + Utils.sms);
                                //await SignalRService
                                //    .Instance
                                //    .ProveedoresHub
                                //    .Invoke(
                                //        "NotificarProveedorActualizado", "UPDATE",
                                //        proveedor.SupplierID);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombreCompania + Utils.nfmfe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombreCompania + Utils.nfmfm);
                            else
                                U.NotificacionError(idyNombreCompania + Utils.nfmmd);
                            BorrarDatosProveedor();
                            CargarValoresOriginales();
                        }
                        else
                            U.NotificacionError(resultado.mensaje);
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al modificar el proveedor: " + ex.Message);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                }
            }
            else if (tabcOperacion.SelectedTab == tbpEliminar)
            {
                if (U.NotificacionQuestion($"[orange]¿Está seguro de eliminar el proveedor con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}?") == DialogResult.Yes)
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.eliminandoRegistro);
                    btnOperacion.Enabled = false;
                    Proveedor proveedor = new Proveedor
                    {
                        SupplierID = int.Parse(txtId.Text),
                        RowVersion = txtId.Tag as byte[]
                    };
                    try
                    {
                        var resultado = await ApiProveedorService.EliminarAsync
                            (proveedor.SupplierID, txtId.Tag as byte[]);
                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se eliminaron {(numRegs < 0 ? 0 : numRegs)} registro(s)");
                            string idyNombre = $"El proveedor con Id: {txtId.Text} - Nombre de compañía: {txtCompañia.Text}:";
                            if (numRegs > 0)
                            {
                                U.NotificacionInformation(idyNombre + Utils.ses);
                                // por algun motivo no se hace la notificacion a través de SignalR, aunque el procedimiento almacenado se ejecute correctamente y retorne el número de registros afectados, por lo que se fuerza la notificación desde el cliente 
                                // ojo en esto difiere de los demas crud, ya busque pero no encuentro que pasa, el caso es que no se notifica a través de SignalR, aunque el procedimiento almacenado se ejecute correctamente y retorne el número de registros afectados, por lo que se fuerza la notificación desde el cliente
                                await SignalRService
                                    .Instance
                                    .ProveedoresHub
                                    .Invoke(
                                        "NotificarProveedorActualizado", "DELETE",
                                        proveedor.SupplierID);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombre + Utils.nfefe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombre + Utils.nfefm);
                            else
                                U.NotificacionError(idyNombre + Utils.nfemd);
                            BorrarDatosProveedor();
                            CargarValoresOriginales();
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
                else
                {
                    btnOperacion.Enabled = true;
                }
            }
        }

        private void CargarValoresOriginales()
        {
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
