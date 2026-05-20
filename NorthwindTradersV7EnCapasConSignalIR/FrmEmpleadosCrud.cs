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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmEmpleadosCrud : Form
    {
        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private EmpleadoBLL _empleadoBLL;
        private EmpleadoService _empleadoService;
        private bool EjecutarConfDgv = true;
        OpenFileDialog openFileDialog;
        internal Dictionary<string, object> valoresOriginales;
        private byte[] fotoOriginalOle = null;
        private bool realizandoBusqueda = false;
        private bool _procesandoSignalR = false;

        private IDisposable _empleadosSubscription;

        public FrmEmpleadosCrud()
        {
            InitializeComponent();
            _empleadoBLL = new EmpleadoBLL(_connectionString);
            _empleadoService = new EmpleadoService(_connectionString);
        }

        private void FrmEmpleadosCrud_Load(object sender, EventArgs e)
        {
            tabcOperacion.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabcOperacion.DrawItem += tabcOperacion_DrawItem;
            panel1.AutoScrollMinSize = new Size(1000, 800);
            DeshabilitarControles();
            LlenarCboPais();
            LlenarCboReportaA();
            Utils.ConfDgv(dgv);
            LlenarDgv(false);
            CargarValoresOriginales();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Action registrarEventos = () =>
            {
                _empleadosSubscription?.Dispose();

                _empleadosSubscription =
                    SignalRService.Instance.EmpleadosHub
                    .On<string, int>(
                        "empleadoActualizado",
                        EmpleadoActualizadoHandler);
            };

            registrarEventos();

            SignalRService.Instance
                .RegistrarSuscripcion(registrarEventos);
        }

        private void EmpleadoActualizadoHandler(string accion, int empleadoId)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                        EmpleadoActualizadoHandler(accion, empleadoId)));
                    return;
                }

                if (realizandoBusqueda)
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
                    int.TryParse(txtId.Text, out int empleadoActual))
                {
                    if (empleadoActual == empleadoId)
                    {
                        CargarEmpleado(empleadoId);
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
                _empleadosSubscription?.Dispose();
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }

        private void tabcOperacion_DrawItem(object sender, DrawItemEventArgs e) => Utils.DibujarPestañas(sender as TabControl, e);

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmEmpleadosCrud_FormClosed(object sender, FormClosedEventArgs e) =>
            MDIPrincipal.ActualizarBarraDeEstado();

        internal void FrmEmpleadosCrud_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppShutdownService.CerrandoPorLogout)
                return;

            // pone un error con errorprovider en cada control que ha cambiado
            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
                if (U.NotificacionQuestion(Utils.preguntaCerrar) == DialogResult.No)
                    e.Cancel = true;
        }

        private void DeshabilitarControles()
        {
            txtNombres.ReadOnly = txtApellidos.ReadOnly = txtTitulo.ReadOnly = txtTitCortesia.ReadOnly = true;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = true;
            txtTelefono.ReadOnly = txtExtension.ReadOnly = true;
            dtpFNacimiento.Enabled = dtpFContratacion.Enabled = false;
            txtNotas.ReadOnly = true;
            cboPais.Enabled = cboReportaA.Enabled = false;
            picFoto.Enabled = false;
            btnCargar.Enabled = false;
            txtNotas.BackColor = SystemColors.Control;
        }

        private void HabilitarControles()
        {
            txtNombres.ReadOnly = txtApellidos.ReadOnly = txtTitulo.ReadOnly = false;
            txtTitCortesia.ReadOnly = false;
            txtDomicilio.ReadOnly = txtCiudad.ReadOnly = txtRegion.ReadOnly = txtCodigoP.ReadOnly = false;
            txtTelefono.ReadOnly = txtExtension.ReadOnly = false;
            txtNotas.ReadOnly = false;
            cboPais.Enabled = dtpFNacimiento.Enabled = dtpFContratacion.Enabled = cboReportaA.Enabled = true;
            picFoto.Enabled = true;
            txtNotas.BackColor = SystemColors.Window;
        }

        private void LlenarCboPais()
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                int employeeId = 0;
                string selectedValueCboPais = cboPais.Text;
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    employeeId = 0;
                    tieneId = int.TryParse(txtId.Text, out employeeId);
                }
                var paises = _empleadoBLL.ObtenerEmpleadosPaisesCbo();

                // Llenar cboBPais
                cboBPais.DataSource = paises;
                cboBPais.ValueMember = "Id";
                cboBPais.DisplayMember = "Pais";
                cboBPais.SelectedIndex = 0;

                // Llenar cboPais
                cboPais.DataSource = paises.ToList();
                cboPais.ValueMember = "Id";
                cboPais.DisplayMember = "Pais";
                cboPais.SelectedIndex = 0;

                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    // 🔹 Restaurar desde BD
                    if (tieneId)
                    {
                        var pais = _empleadoService.ObtenerEmpleadoPais(employeeId);
                        // paises es una colección, pero solo debe haber uno o ninguno, así que buscamos el primero que coincida
                        if (pais != null &&
                            paises.Any(p => p.Id == pais.Id))
                        {
                            cboPais.SelectedValue = pais.Id;
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
                                    cboPais.Text = selectedValueCboPais;
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

        private void LlenarCboReportaA()
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                int employeeId = 0;
                string selectedValueCboReportaA = cboReportaA.SelectedValue?.ToString();
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    employeeId = 0;
                    tieneId = int.TryParse(txtId.Text, out employeeId);
                }

                var empleados = _empleadoBLL.ObtenerEmpleadoReportaaCbo();
                cboReportaA.DataSource = empleados;
                cboReportaA.ValueMember = "Id";
                cboReportaA.DisplayMember = "Nombre";
                cboReportaA.SelectedIndex = 0;

                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    // 🔹 Restaurar desde BD
                    if (tieneId)
                    {
                        var jefe = _empleadoService.ObtenerEmpleadoReportaA(employeeId);
                        if (jefe != null)
                        {
                            // jefe es un objeto único, no colección
                            cboReportaA.SelectedValue = jefe.EmployeeId;
                        }
                        else
                        {
                            if (selectedValueCboReportaA != null)
                                cboReportaA.SelectedValue = selectedValueCboReportaA;
                            else
                                cboReportaA.SelectedIndex = 0;
                        }
                    }
                }
                if ((tabcOperacion.SelectedTab == tbpRegistrar || tabcOperacion.SelectedTab == tbpEliminar || tabcOperacion.SelectedTab == tbpListar) && selectedValueCboReportaA != null)
                {
                    cboReportaA.SelectedValue = selectedValueCboReportaA;
                }
                CargarValoresOriginales();
                MDIPrincipal.ActualizarBarraDeEstado();
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        private void LlenarDgv(bool selectorRealizaBusqueda)
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                DtoEmpleadosBuscar criterios = new DtoEmpleadosBuscar
                {
                    IdIniTxt = txtBIdIni.Text,
                    IdFinTxt = txtBIdFin.Text,
                    Nombres = txtBNombres.Text.Trim(),
                    Apellidos = txtBApellidos.Text.Trim(),
                    Titulo = txtBTitulo.Text.Trim(),
                    Domicilio = txtBDomicilio.Text.Trim(),
                    Ciudad = txtBCiudad.Text.Trim(),
                    Region = txtBRegion.Text.Trim(),
                    CodigoP = txtBCodigoP.Text.Trim(),
                    Pais = cboBPais.SelectedValue.ToString(),
                    Telefono = txtBTelefono.Text.Trim()
                };
                var resultado = _empleadoBLL.ObtenerEmpleadosDgv(selectorRealizaBusqueda, criterios);
                dgv.DataSource = resultado.empleados;
                if (EjecutarConfDgv)
                {
                    ConfDgv();
                    EjecutarConfDgv = false;
                }
                LlenarCombos();
                MDIPrincipal.ActualizarBarraDeEstado(resultado.mensajeEstado);
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        void ConfDgv()
        {
            dgv.Columns["EmployeeID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Title"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["BirthDate"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["City"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Country"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["ReportsToName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            dgv.Columns["Photo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns["Photo"].Width = 80;
            dgv.RowTemplate.Height = 80;
            dgv.Columns["Photo"].DefaultCellStyle.Padding = new Padding(4);
            ((DataGridViewImageColumn)dgv.Columns["Photo"]).ImageLayout = DataGridViewImageCellLayout.Zoom;

            dgv.Columns["Title"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["BirthDate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["City"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Country"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["ReportsToName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["BirthDate"].DefaultCellStyle.Format = "dd \" de \"MMM\" de \"yyyy";

            dgv.Columns["EmployeeID"].HeaderText = "Id";
            dgv.Columns["FirstName"].HeaderText = "Nombres";
            dgv.Columns["LastName"].HeaderText = "Apellidos";
            dgv.Columns["Title"].HeaderText = "Título";
            dgv.Columns["BirthDate"].HeaderText = "Fecha de nacimiento";
            dgv.Columns["City"].HeaderText = "Ciudad";
            dgv.Columns["Country"].HeaderText = "País";
            dgv.Columns["Photo"].HeaderText = "Foto";
            dgv.Columns["ReportsToName"].HeaderText = "Reporta a";
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BorrarDatosEmpleado();
            BorrarMensajesError();
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
            BorrarDatosEmpleado();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(false);
            CargarValoresOriginales();
            realizandoBusqueda = false;
        }

        void BorrarMensajesError() => errorProvider1.Clear();

        void BorrarDatosBusqueda()
        {
            txtBIdIni.Text = txtBIdFin.Text = txtBNombres.Text = txtBApellidos.Text = string.Empty;
            txtBTitulo.Text = txtBDomicilio.Text = txtBCiudad.Text = string.Empty;
            txtBRegion.Text = txtBCodigoP.Text = txtBTelefono.Text = string.Empty;
            cboBPais.SelectedIndex = 0;
        }

        void BorrarDatosEmpleado()
        {
            txtId.Text = txtNombres.Text = txtApellidos.Text = txtTitulo.Text = string.Empty;
            txtTitCortesia.Text = txtDomicilio.Text = txtCiudad.Text = string.Empty;
            txtRegion.Text = txtCodigoP.Text = txtTelefono.Text = string.Empty;
            txtExtension.Text = txtNotas.Text = string.Empty;
            cboPais.Text = null;
            cboPais.SelectedIndex = cboReportaA.SelectedIndex = 0;
            picFoto.Image = Properties.Resources.FotoPerfil;
            dtpFNacimiento.Value = dtpFNacimiento.MinDate;
            dtpFContratacion.Value = dtpFContratacion.MinDate;
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
            if (txtNombres.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtNombres, "Ingrese el nombre");
            }
            if (txtApellidos.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtApellidos, "Ingrese el apellido");
            }
            if (txtTitulo.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtTitulo, "Ingrese el título");
            }
            if (txtTitCortesia.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtTitCortesia, "Ingrese el título de cortesia");
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
            //if (cboPais.Text.Trim() == "" || cboPais.SelectedIndex == 0)
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
            if (picFoto.Image == null)
            {
                valida = false;
                errorProvider1.SetError(btnCargar, "Ingrese la foto");
            }
            if (dtpFNacimiento.Value == new DateTime(1753, 1, 1))
            {
                valida = false;
                errorProvider1.SetError(dtpFNacimiento, "Ingrese la fecha de nacimiento");
            }
            if (dtpFContratacion.Value == new DateTime(1753, 1, 1))
            {
                valida = false;
                errorProvider1.SetError(dtpFContratacion, "Ingrese la fecha de contratación");
            }
            if (cboReportaA.SelectedValue == null || cboReportaA.SelectedIndex == 0)
            {
                valida = false;
                errorProvider1.SetError(cboReportaA, "Seleccione a quien reporta el empleado");
            }
            return valida;
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 🔹 Cuando viene desde un click real del DataGridView
            if (e != null)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                DataGridViewRow dgvr = dgv.Rows[e.RowIndex];

                if (dgvr.Cells["EmployeeID"].Value == null)
                    return;

                txtId.Text = dgvr.Cells["EmployeeID"].Value.ToString();
            }
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
            {
                DeshabilitarControles();
                try
                {
                    int employeeId = Convert.ToInt32(txtId.Text);
                    if (!CargarEmpleado(employeeId))
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
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = true;
                    btnCargar.Visible = false;
                }
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    HabilitarControles();
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = true;
                    btnCargar.Visible = true;
                }
                if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Enabled = true;
                    btnOperacion.Visible = true;
                    btnCargar.Visible = false;
                }
            }
            CargarValoresOriginales();
        }

        private void dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // debe estar vinculado a la clase List<> a la cual esta vinculado el DataGridView.DataSource
            Utils.OrdenarPorColumna<DtoEmpleadosDgv>(dgv, e);
        }

        void ActualizaDgv() => btnLimpiar.PerformClick();

        private void tabcOperacion_Selected(object sender, TabControlEventArgs e)
        {
            BorrarDatosEmpleado();
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                BorrarDatosBusqueda();
                HabilitarControles();
                btnOperacion.Text = "Registrar empleado";
                btnOperacion.Visible = true;
                btnOperacion.Enabled = true;
                btnCargar.Enabled = true;
                btnCargar.Visible = true;
                cboReportaA.SelectedIndex = 0;
            }
            else
            {
                dgv.CellClick -= new DataGridViewCellEventHandler(dgv_CellClick);
                dgv.CellClick += new DataGridViewCellEventHandler(dgv_CellClick);
                DeshabilitarControles();
                btnOperacion.Enabled = false;
                btnCargar.Enabled = false;
                if (tabcOperacion.SelectedTab == tbpListar)
                {
                    btnOperacion.Text = "Imprimir empleado";
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = false;
                    btnCargar.Visible = false;
                    btnCargar.Enabled = false;
                }
                else if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    btnOperacion.Text = "Modificar empleado";
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = false;
                    btnCargar.Visible = true;
                    btnCargar.Enabled = false;
                }
                else if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Text = "Eliminar empleado";
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = false;
                    btnCargar.Visible = false;
                    btnCargar.Enabled = false;
                }
            }
            CargarValoresOriginales();
        }

        private void tabcOperacion_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
                if (U.NotificacionQuestion(Utils.preguntaCerrarPestaña) == DialogResult.No)
                    e.Cancel = true;
        }

        private void btnCargar_Click(object sender, EventArgs e)
        {
            // Mostrar el cuadro de diálogo OpenFileDialog
            //La instrucción siguiente es para que nos muestre todos los tipos juntos
            openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Archivos de imagen (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog.InitialDirectory = "c:\\Imágenes\\";
            //La instrucción siguiente es para que nos muestre varias filas en el openfiledialog que nos permita abrir por un tipo especifico
            openFileDialog.Filter = "Archivos jpg (*.jpg)|*.jpg|Archivos jpeg (*.jpeg)|*.jpeg|Archivos png (*.png)|*.png|Archivos bmp (*.bmp)|*.bmp";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Cargar la imagen seleccionada en un objeto Image
                Image image = Image.FromFile(openFileDialog.FileName);

                // Mostrar la imagen en un control PictureBox
                picFoto.Image = image;
                errorProvider1.SetError(btnCargar, "");
            }
        }

        private void LlenarCombos()
        {
            LlenarCboPais();
            LlenarCboReportaA();
        }

        private async void btnOperacion_Click(object sender, EventArgs e)
        {
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab == tbpListar)
            {
                FrmRptEmpleado frmRptEmpleado = new FrmRptEmpleado();
                frmRptEmpleado.Id = int.Parse(txtId.Text);
                frmRptEmpleado.ShowDialog();
                return;
            }
            else if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                if (ValidarControles())
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.insertandoRegistro);
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    try
                    {
                        var empleado = new Empleado
                        {
                            FirstName = txtNombres.Text.Trim(),
                            LastName = txtApellidos.Text.Trim(),
                            Title = txtTitulo.Text.Trim(),
                            TitleOfCourtesy = txtTitCortesia.Text.Trim(),
                            BirthDate = dtpFNacimiento.Value == dtpFNacimiento.MinDate ? (DateTime?)null : dtpFNacimiento.Value,
                            HireDate = dtpFContratacion.Value == dtpFContratacion.MinDate ? (DateTime?)null : dtpFContratacion.Value,
                            Address = txtDomicilio.Text.Trim(),
                            City = txtCiudad.Text.Trim(),
                            Region = txtRegion.Text.Trim(),
                            PostalCode = txtCodigoP.Text.Trim(),
                            Country = cboPais.Text.Trim(),
                            HomePhone = txtTelefono.Text.Trim(),
                            Extension = txtExtension.Text.Trim(),
                            Notes = txtNotas.Text.Trim(),
                            ReportsTo = cboReportaA.SelectedValue.ToString() == "0" ? (int?)null : Convert.ToInt32(cboReportaA.SelectedValue),
                            Photo = picFoto.Image != null ? Utils.ImageToByteArray(picFoto.Image) : null
                        };

                        var resultado = await ApiEmpleadoService.InsertarAsync(empleado);
                        if (resultado.ok)
                        {
                            txtId.Text = resultado.empleado.EmployeeID.ToString();
                            string idyNombre =
                                $"El empleado con Id: {txtId.Text} - Nombre: {txtNombres.Text} {txtApellidos.Text}:";
                            MDIPrincipal.ActualizarBarraDeEstado(
                                $"Se insertó 1 registro");
                            U.NotificacionInformation(
                                idyNombre + Utils.srs);
                            await SignalRService.Instance
                                .EmpleadosHub
                                .Invoke(
                                    "NotificarEmpleadoActualizado",
                                    "INSERT",
                                    resultado.empleado.EmployeeID);
                        }
                        else
                        {
                            U.NotificacionError(resultado.mensaje);
                        }        
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al insertar el empleado: " + ex.Message);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                    HabilitarControles();
                    btnOperacion.Enabled = true;
                    btnCargar.Enabled = true;
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
                        var empleado = new Empleado
                        {
                            EmployeeID = Convert.ToInt32(txtId.Text),
                            FirstName = txtNombres.Text.Trim(),
                            LastName = txtApellidos.Text.Trim(),
                            Title = txtTitulo.Text.Trim(),
                            TitleOfCourtesy = txtTitCortesia.Text.Trim(),
                            BirthDate = dtpFNacimiento.Value == dtpFNacimiento.MinDate ? (DateTime?)null : dtpFNacimiento.Value,
                            HireDate = dtpFContratacion.Value == dtpFContratacion.MinDate ? (DateTime?)null : dtpFContratacion.Value,
                            Address = txtDomicilio.Text.Trim(),
                            City = txtCiudad.Text.Trim(),
                            Region = txtRegion.Text.Trim(),
                            PostalCode = txtCodigoP.Text.Trim(),
                            Country = cboPais.Text.Trim(),
                            HomePhone = txtTelefono.Text.Trim(),
                            Extension = txtExtension.Text.Trim(),
                            Notes = txtNotas.Text.Trim(),
                            ReportsTo = cboReportaA.SelectedValue.ToString() == "0" ? (int?)null : Convert.ToInt32(cboReportaA.SelectedValue),
                            RowVersion = txtId.Tag as byte[]
                        };
                        if (Convert.ToInt32(txtId.Text) <= 9)
                        {
                            empleado.Photo = fotoOriginalOle; // conservas el OLE original
                        }
                        else
                        {
                            empleado.Photo = Utils.ImageToByteArray(picFoto.Image);
                        }
                        var valorPais = cboPais.SelectedValue?.ToString();

                        var resultado = await ApiEmpleadoService.ActualizarAsync(empleado);

                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;

                            MDIPrincipal.ActualizarBarraDeEstado(
                                $"Se actualizó {(numRegs < 0 ? 0 : numRegs)} registro");

                            string idyNombre =
                                $"El empleado con Id: {txtId.Text} - Nombre: {txtNombres.Text} {txtApellidos.Text}:";

                            if (numRegs > 0)
                            { 
                                U.NotificacionInformation(idyNombre + Utils.sms);
                                await SignalRService.Instance
                                    .EmpleadosHub
                                    .Invoke(
                                        "NotificarEmpleadoActualizado",
                                        "UPDATE",
                                        empleado.EmployeeID);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombre + Utils.nfmfe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombre + Utils.nfmfm);
                            else
                                U.NotificacionError(idyNombre + Utils.nfmmd);
                        }
                        else
                        {
                            U.NotificacionError(resultado.mensaje);
                        }
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al modificar el empleado: " + ex.Message);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                }
            }
            else if (tabcOperacion.SelectedTab == tbpEliminar)
            {
                if (U.NotificacionQuestion($"[orange]¿Está seguro de eliminar el empleado con Id: {txtId.Text} - Nombre: {txtNombres.Text} {txtApellidos.Text}?") == DialogResult.Yes)
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.eliminandoRegistro);
                    btnOperacion.Enabled = false;
                    var empleado = new Empleado
                    {
                        EmployeeID = Convert.ToInt32(txtId.Text),
                        RowVersion = txtId.Tag as byte[]
                    };
                    try
                    {
                        var resultado = await ApiEmpleadoService.EliminarAsync(
                            Convert.ToInt32(txtId.Text),
                            txtId.Tag as byte[]);

                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;

                            MDIPrincipal.ActualizarBarraDeEstado(
                                $"Se eliminó {(numRegs < 0 ? 0 : numRegs)} registro");

                            string idyNombre =
                                $"El empleado con Id: {txtId.Text} - Nombre: {txtNombres.Text} {txtApellidos.Text}:";

                            if (numRegs > 0)
                            {
                                U.NotificacionInformation(idyNombre + Utils.ses);
                                await SignalRService.Instance
                                    .EmpleadosHub
                                    .Invoke(
                                        "NotificarEmpleadoActualizado",
                                        "DELETE",
                                        empleado.EmployeeID);
                            }
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombre + Utils.nfefe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombre + Utils.nfefm);
                            else
                                U.NotificacionError(idyNombre + Utils.nfemd);
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
                    btnOperacion.Enabled = false;
                }
            }
            BorrarDatosEmpleado();
            CargarValoresOriginales();
        }

        private void CargarValoresOriginales()
        {
            valoresOriginales = Utils.CapturarValoresOriginales(this);
        }

        private bool CargarEmpleado(int employeeId)
        {
            try
            {
                var empleado =
                    _empleadoBLL.ObtenerEmpleadoPorId(employeeId);
                if (empleado == null)
                {
                    U.NotificacionWarning(
                        $"No se encontró el empleado con Id: {employeeId}." + Utils.erfep1);
                    BorrarDatosEmpleado();
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    CargarValoresOriginales();
                    return false;
                }
                txtId.Text = empleado.EmployeeID.ToString();
                if (empleado.BirthDate != null)
                    dtpFNacimiento.Value = empleado.BirthDate.Value;
                else
                    dtpFNacimiento.Value = dtpFNacimiento.MinDate;
                if (empleado.HireDate != null)
                    dtpFContratacion.Value = empleado.HireDate.Value;
                else
                    dtpFContratacion.Value = dtpFContratacion.MinDate;
                if (empleado.Photo != null)
                {
                    fotoOriginalOle = empleado.Photo;
                    using (var ms = new MemoryStream(empleado.Photo))
                        picFoto.Image = Image.FromStream(ms);
                    if (empleado.EmployeeID <= 9)
                        btnCargar.Enabled = false;
                    else
                        btnCargar.Enabled = true;
                }
                else
                {
                    picFoto.Image = null;
                    btnCargar.Enabled = true;
                }
                if (empleado.ReportsTo != null)
                    cboReportaA.SelectedValue = empleado.ReportsTo.Value;
                else
                    cboReportaA.SelectedValue = 0;
                txtId.Tag = empleado.RowVersion;
                txtNombres.Text = empleado.FirstName;
                txtApellidos.Text = empleado.LastName;
                txtTitulo.Text = empleado.Title;
                txtTitCortesia.Text = empleado.TitleOfCourtesy;
                txtDomicilio.Text = empleado.Address;
                txtCiudad.Text = empleado.City;
                txtRegion.Text = empleado.Region;
                txtCodigoP.Text = empleado.PostalCode;
                cboPais.Text = empleado.Country;
                txtTelefono.Text = empleado.HomePhone;
                txtExtension.Text = empleado.Extension;
                txtNotas.Text = empleado.Notes;
                CargarValoresOriginales();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
