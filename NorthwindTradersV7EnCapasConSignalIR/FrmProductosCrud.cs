using BLL;
using BLL.Services;
using Entities;
using Entities.DTOs;
using Infrastructure.Services;
using Microsoft.AspNet.SignalR.Client;
using NorthwindTradersV7EnCapasConSignalIR.Helpers;
using NorthwindTradersV7EnCapasConSignalIR.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmProductosCrud : Form
    {
        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private ProductoBLL _productoBLL;
        private readonly CategoriaService _categoriaService;
        private readonly ProveedorService _proveedorService;
        private readonly ProductoService _productoService;
        private bool EjecutarConfDgv = true;
        internal Dictionary<string, object> valoresOriginales;
        private bool _realizandoBusqueda = false;
        private bool _procesandoSignalR = false;

        private IDisposable _productosSubscription;
        
        public FrmProductosCrud()
        {
            InitializeComponent();

            _productoBLL = new ProductoBLL(_connectionString);
            _categoriaService = new CategoriaService(_connectionString);
            _proveedorService = new ProveedorService(_connectionString);
            _productoService = new ProductoService(_connectionString);
        }

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmProductosCrud_FormClosed(object sender, FormClosedEventArgs e) => MDIPrincipal.ActualizarBarraDeEstado();

        internal void FrmProductosCrud_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AppShutdownService.CerrandoPorLogout)
                return;

            if (Utils.HayCambios(this, valoresOriginales, errorProvider1))
                if (U.NotificacionQuestion(Utils.preguntaCerrar) == DialogResult.No)
                    e.Cancel = true;
        }

        private void tabcOperacion_DrawItem(object sender, DrawItemEventArgs e) => Utils.DibujarPestañas(sender as TabControl, e);

        private void FrmProductosCrud_Load(object sender, EventArgs e)
        {
            tabcOperacion.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabcOperacion.DrawItem += tabcOperacion_DrawItem;
            // Obtener el símbolo de moneda según la configuración regional del equipo
            string simboloMoneda = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
            // Mostrarlo en el Label
            LblPrecio.Text = "Precio " + simboloMoneda + ":";
            DeshabilitarControles();
            LlenarCboCategoria();
            LlenarCboProveedor();
            Utils.ConfDgv(Dgv);
            LlenarDgv(false);
            CargarValoresOriginales();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Action registrarEventos = () =>
            {
                _productosSubscription?.Dispose();

                _productosSubscription =
                    SignalRService.Instance.ProductosHub
                    .On<string, int>(
                        "productoActualizado",
                        ProductoActualizadoHandler);
            };

            registrarEventos();

            SignalRService.Instance
                .RegistrarSuscripcion(registrarEventos);
        }

        private void ProductoActualizadoHandler(string accion, int productoId)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                        ProductoActualizadoHandler(accion, productoId)));
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
                    int.TryParse(txtId.Text, out int productoActual))
                {
                    if (productoActual == productoId)
                    {
                        CargarProducto(productoId);
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
                _productosSubscription?.Dispose();
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }

        private bool CargarProducto(int productoId)
        {
            try
            {
                var producto =
                    _productoBLL.ObtenerProductoPorId(productoId);
                if (producto == null)
                {
                    U.NotificacionWarning(
                        $"No se encontró el producto con Id: {productoId}." + Utils.erfep1);
                    BorrarDatosProducto();
                    DeshabilitarControles();
                    btnOperacion.Enabled = false;
                    CargarValoresOriginales();
                    return false;
                }
                txtId.Text = producto.ProductID.ToString();
                txtId.Tag = producto.RowVersion;
                cboCategoria.SelectedValue = producto.Categoria?.CategoryID ?? 0;
                cboProveedor.SelectedValue = producto.Proveedor?.SupplierID ?? 0;
                txtProducto.Text = producto.ProductName ?? "";
                txtCantidadxU.Text = producto.QuantityPerUnit ?? "";
                nudPrecio.Value = producto.UnitPrice ?? 0m;
                nudUInventario.Value = producto.UnitsInStock ?? 0;
                nudUPedido.Value = producto.UnitsOnOrder ?? 0;
                nudPPedido.Value = producto.ReorderLevel ?? 0;
                chkbDescontinuado.Checked = producto.Discontinued;
                CargarValoresOriginales();
                return true;
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
                return false;
            }
        }

        private void DeshabilitarNuds()
        {
            Utilities.NudHelper.SetEnabled(nudPrecio, false);
            Utilities.NudHelper.SetEnabled(nudUInventario, false);
            Utilities.NudHelper.SetEnabled(nudUPedido, false);
            Utilities.NudHelper.SetEnabled(nudPPedido, false);
        }

        private void HabilitarNuds()
        {
            Utilities.NudHelper.SetEnabled(nudPrecio, true);
            Utilities.NudHelper.SetEnabled(nudUInventario, true);
            Utilities.NudHelper.SetEnabled(nudUPedido, true);
            Utilities.NudHelper.SetEnabled(nudPPedido, true);
        }

        private void DeshabilitarControles()
        {
            txtProducto.ReadOnly = txtCantidadxU.ReadOnly = true;
            chkbDescontinuado.Enabled = false;
            cboCategoria.Enabled = cboProveedor.Enabled = false;
            DeshabilitarNuds();
        }

        private void HabilitarControles()
        {
            txtProducto.ReadOnly = txtCantidadxU.ReadOnly = false;
            chkbDescontinuado.Enabled = true;
            cboCategoria.Enabled = cboProveedor.Enabled = true;
            HabilitarNuds();
        }

        private void LlenarCboCategoria()
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                int productId = 0;
                string selectedValueCboCategoria = cboCategoria.SelectedValue?.ToString();
                if (tabcOperacion.SelectedTab == tbpModificar) 
                { 
                    productId = 0;
                    tieneId = int.TryParse(txtId.Text, out productId);
                }
                var dtCboCategoria = _categoriaService.ObtenerCategoriasCbo();
                var dtBCboCategoria = dtCboCategoria.Copy();
                ComboBoxHelper.LlenarCbo(cboCategoria, dtCboCategoria, "CategoryName", "CategoryID");
                ComboBoxHelper.LlenarCbo(cboBCategoria, dtBCboCategoria, "CategoryName", "CategoryID");
                if (tabcOperacion.SelectedTab == tbpModificar && tieneId)
                {
                    var categoriaId = _productoService.ObtenerProductoCategoriaId(productId);
                    if (categoriaId != 0)
                        cboCategoria.SelectedValue = categoriaId;
                    else
                    {
                        if (selectedValueCboCategoria != null)
                            cboCategoria.SelectedValue = selectedValueCboCategoria;
                        else
                            cboCategoria.SelectedIndex = 0;
                    }
                }
                if ((tabcOperacion.SelectedTab == tbpRegistrar || tabcOperacion.SelectedTab == tbpEliminar || tabcOperacion.SelectedTab == tbpConsultar) && selectedValueCboCategoria != null)
                {
                    cboCategoria.SelectedValue = selectedValueCboCategoria;
                }
                CargarValoresOriginales();
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

        private void LlenarCboProveedor()
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                bool tieneId = false;
                int productId = 0;
                string selectedValueCboProveedor = cboProveedor.SelectedValue?.ToString();
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    productId = 0;
                    tieneId = int.TryParse(txtId.Text, out productId);
                }
                var dtCboProveedor = _proveedorService.ObtenerProveedoresCbo();
                var dtBCboProveedor = dtCboProveedor.Copy();
                ComboBoxHelper.LlenarCbo(cboProveedor, dtCboProveedor, "CompanyName", "SupplierId");
                ComboBoxHelper.LlenarCbo(cboBProveedor, dtBCboProveedor, "CompanyName", "SupplierId");
                if (tabcOperacion.SelectedTab == tbpModificar && tieneId)
                {
                    var proveedorId = _productoService.ObtenerProductoProveedorId(productId);
                    if (proveedorId != 0)
                        cboProveedor.SelectedValue = proveedorId;
                    else
                    {
                        if (selectedValueCboProveedor != null)
                            cboProveedor.SelectedValue = selectedValueCboProveedor;
                        else
                            cboProveedor.SelectedIndex = 0;
                    }
                }
                if ((tabcOperacion.SelectedTab == tbpRegistrar || tabcOperacion.SelectedTab == tbpEliminar || tabcOperacion.SelectedTab == tbpConsultar) && selectedValueCboProveedor != null)
                {
                    cboProveedor.SelectedValue = selectedValueCboProveedor;
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

        private void LlenarDgv(bool selectorRealizaBusqueda)
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                DtoProductosBuscar criterios;
                if (selectorRealizaBusqueda)
                    criterios = new DtoProductosBuscar()
                    {
                        IdIni = Convert.ToInt32(nudBIdIni.Value),
                        IdFin = Convert.ToInt32(nudBIdFin.Value),
                        Producto = txtBProducto.Text.Trim(),
                        Categoria = cboBCategoria.SelectedValue == null ? 0 : Convert.ToInt32(cboBCategoria.SelectedValue),
                        Proveedor = cboBProveedor.SelectedValue == null ? 0 : Convert.ToInt32(cboBProveedor.SelectedValue)
                    };
                else
                    criterios = null;
                var productos = _productoBLL.ObtenerProductos(selectorRealizaBusqueda, criterios, false);
                var dtoProductos = productos.Select(p => new DtoProducto
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitPrice = p.UnitPrice,
                    UnitsInStock = p.UnitsInStock,
                    UnitsOnOrder = p.UnitsOnOrder,
                    ReorderLevel = p.ReorderLevel,
                    Discontinued = p.Discontinued,
                    CategoryName = p.Categoria?.CategoryName,
                    Description = p.Categoria?.Description,
                    CompanyName = p.Proveedor?.CompanyName,
                    CategoryID = p.Categoria?.CategoryID ?? 0,
                    SupplierID = p.Proveedor?.SupplierID ?? 0
                }).ToList();
                Dgv.DataSource = dtoProductos;
                if (EjecutarConfDgv)
                {
                    ConfDgv();
                    EjecutarConfDgv = false;
                }
                LlenarCombos();
                if (selectorRealizaBusqueda)
                    MDIPrincipal.ActualizarBarraDeEstado($"Se encontraron {Dgv.RowCount} registro(s)");
                else
                    MDIPrincipal.ActualizarBarraDeEstado($"Se muestran los últimos {Dgv.RowCount} producto(s) registrado(s)");
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }

        private void ConfDgv()
        {
            Dgv.Columns["CategoryID"].Visible = false;
            Dgv.Columns["SupplierID"].Visible = false;

            Dgv.Columns["ProductID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["UnitPrice"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["UnitsInStock"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["UnitsOnOrder"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["ReorderLevel"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["Discontinued"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            Dgv.Columns["CategoryName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            Dgv.Columns["UnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["UnitsInStock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["UnitsOnOrder"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Dgv.Columns["ReorderLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            Dgv.Columns["UnitPrice"].DefaultCellStyle.Format = "c";
            Dgv.Columns["UnitsInStock"].DefaultCellStyle.Format = "N0";
            Dgv.Columns["UnitsOnOrder"].DefaultCellStyle.Format = "N0";
            Dgv.Columns["ReorderLevel"].DefaultCellStyle.Format = "N0";

            Dgv.Columns["ProductID"].HeaderText = "Id";
            Dgv.Columns["ProductName"].HeaderText = "Producto";
            Dgv.Columns["QuantityPerUnit"].HeaderText = "Cantidad por unidad";
            Dgv.Columns["UnitPrice"].HeaderText = "Precio";
            Dgv.Columns["UnitsInStock"].HeaderText = "Unidades en inventario";
            Dgv.Columns["UnitsOnOrder"].HeaderText = "Unidades en pedido";
            Dgv.Columns["ReorderLevel"].HeaderText = "Nivel de reorden";
            Dgv.Columns["Discontinued"].HeaderText = "Descontinuado";
            Dgv.Columns["CategoryName"].HeaderText = "Categoría";
            Dgv.Columns["Description"].HeaderText = "Descripción de categoría";
            Dgv.Columns["CompanyName"].HeaderText = "Proveedor";
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BorrarDatosProducto();
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(true);
            CargarValoresOriginales();
            _realizandoBusqueda = true;
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            BorrarDatosProducto();
            BorrarMensajesError();
            BorrarDatosBusqueda();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
                DeshabilitarControles();
            LlenarDgv(false);
            CargarValoresOriginales();
            _realizandoBusqueda = false;
        }

        private void BorrarDatosProducto()
        {
            txtId.Text = txtProducto.Text = txtCantidadxU.Text = "";
            nudPPedido.Value = nudUPedido.Value = nudUInventario.Value = nudPrecio.Value = 0;
            chkbDescontinuado.Checked = false;
            cboCategoria.SelectedIndex = cboProveedor.SelectedIndex = 0;
        }

        private void BorrarMensajesError() => errorProvider1.Clear();

        private void BorrarDatosBusqueda()
        {
            nudBIdIni.Value = nudBIdFin.Value = 0;
            txtBProducto.Text = "";
            cboBCategoria.SelectedIndex = cboBProveedor.SelectedIndex = 0;
        }

        private bool ValidarControles()
        {
            bool valida = true;
            if (cboCategoria.SelectedIndex == 0 || cboCategoria.SelectedIndex == -1)
            {
                valida = false;
                errorProvider1.SetError(cboCategoria, "Seleccione una categoría");
            }
            if (cboProveedor.SelectedIndex == 0 || cboProveedor.SelectedIndex == -1)
            {
                valida = false;
                errorProvider1.SetError(cboProveedor, "Seleccione un proveedor");
            }
            if (txtProducto.Text.Trim() == "")
            {
                valida = false;
                errorProvider1.SetError(txtProducto, "Ingrese producto");
            }
            if (nudPrecio.Value == 0)
            {
                valida = false;
                errorProvider1.SetError(nudPrecio, "Ingrese precio");
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

                if (dgvr.Cells["ProductID"].Value == null)
                    return;

                txtId.Text = dgvr.Cells["ProductID"].Value.ToString();
            }
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab != tbpRegistrar)
            {
                DeshabilitarControles();
                try
                {
                    int productId = Convert.ToInt32(txtId.Text);
                    if (!CargarProducto(productId))
                    {
                        ActualizaDgv();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    U.MsgCatchOue(ex);
                }
                if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    HabilitarControles();
                    btnOperacion.Enabled = true;
                }
                else if (tabcOperacion.SelectedTab == tbpEliminar)
                    btnOperacion.Enabled = true;
            }
            CargarValoresOriginales();
        }

        private void Dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // debe estar vinculado a la clase List<> a la cual esta vinculado el DataGridView.DataSource
            Utils.OrdenarPorColumna<DtoProducto>(Dgv, e);
        }

        private void tabcOperacion_Selected(object sender, TabControlEventArgs e)
        {
            BorrarDatosProducto();
            BorrarMensajesError();
            if (tabcOperacion.SelectedTab == tbpRegistrar)
            {
                Dgv.CellClick -= new DataGridViewCellEventHandler(Dgv_CellClick);
                Dgv.CellClick -= new DataGridViewCellEventHandler(Dgv_CellClick);
                BorrarDatosBusqueda();
                HabilitarControles();
                btnOperacion.Text = "Registrar producto";
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
                {
                    btnOperacion.Visible = false;
                    btnOperacion.Enabled = false;
                }
                else if (tabcOperacion.SelectedTab == tbpModificar)
                {
                    btnOperacion.Text = "Modificar producto";
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = false;
                }
                else if (tabcOperacion.SelectedTab == tbpEliminar)
                {
                    btnOperacion.Text = "Eliminar producto";
                    btnOperacion.Visible = true;
                    btnOperacion.Enabled = false;
                }
            }
            CargarValoresOriginales();
        }

        private void Nud_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Bloquear punto y coma
            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                e.Handled = true; // Ignora la tecla
            }
        }

        private void Nud_Enter(object sender, EventArgs e)
        {
            if (sender is NumericUpDown nud && nud.Controls[1] is TextBox tb)
            {
                // Diferir la selección para que ocurra después de que el TextBox reciba el foco
                tb.BeginInvoke((Action)(() => tb.SelectAll()));
            }
        }

        private void nudBIdIni_Leave(object sender, EventArgs e) => Utils.ValidarRango(sender, nudBIdIni, nudBIdFin);

        private void nudBIdFin_Leave(object sender, EventArgs e) => Utils.ValidarRango(sender, nudBIdIni, nudBIdFin);

        private void nudBIdIni_ValueChanged(object sender, EventArgs e) => Utils.ValidarRango(sender, nudBIdIni, nudBIdFin);

        private void nudBIdFin_ValueChanged(object sender, EventArgs e) => Utils.ValidarRango(sender, nudBIdIni, nudBIdFin);

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
                        var producto = new Producto
                        {
                            // Relación con Categoria
                            Categoria = new Categoria
                            {
                                CategoryID = Convert.ToInt32(cboCategoria.SelectedValue)
                            },
                            // Relación con Proveedor
                            Proveedor = new Proveedor
                            {
                                SupplierID = Convert.ToInt32(cboProveedor.SelectedValue)
                            },
                            ProductName = txtProducto.Text,
                            QuantityPerUnit = string.IsNullOrEmpty(txtCantidadxU.Text) ? null : txtCantidadxU.Text,
                            UnitPrice = nudPrecio.Value,
                            UnitsInStock = Convert.ToInt16(nudUInventario.Value),
                            UnitsOnOrder = Convert.ToInt16(nudUPedido.Value),
                            ReorderLevel = Convert.ToInt16(nudPPedido.Value),
                            Discontinued = chkbDescontinuado.Checked
                        };
                        var resultado = await ApiProductoService.InsertarAsync(producto);
                        if (resultado.ok)
                        {
                            txtId.Text = resultado.producto.ProductID.ToString();
                            string idNombreProducto = $"El producto con Id: {txtId.Text} - Nombre de producto: {txtProducto.Text}:";
                            MDIPrincipal.ActualizarBarraDeEstado($"Se insertó 1 registro");
                            U.NotificacionInformation(idNombreProducto + Utils.srs);
                            BorrarDatosProducto();
                            CargarValoresOriginales();
                        }
                        else
                        {
                            U.NotificacionError(resultado.mensaje);
                        }
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al insertar el producto: " + ex.Message);
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
                        var producto = new Producto
                        {
                            ProductID = int.Parse(txtId.Text),
                            // Relación con Categoria
                            Categoria = new Categoria
                            {
                                CategoryID = Convert.ToInt32(cboCategoria.SelectedValue)
                            },
                            // Relación con Proveedor
                            Proveedor = new Proveedor
                            {
                                SupplierID = Convert.ToInt32(cboProveedor.SelectedValue)
                            },
                            ProductName = txtProducto.Text,
                            QuantityPerUnit = string.IsNullOrEmpty(txtCantidadxU.Text) ? null : txtCantidadxU.Text,
                            UnitPrice = nudPrecio.Value,
                            UnitsInStock = Convert.ToInt16(nudUInventario.Value),
                            UnitsOnOrder = Convert.ToInt16(nudUPedido.Value),
                            ReorderLevel = Convert.ToInt16(nudPPedido.Value),
                            Discontinued = chkbDescontinuado.Checked,
                            RowVersion = (byte[])txtId.Tag
                        };
                        var resultado = await ApiProductoService.ActualizarAsync(producto);
                        if (resultado.ok)
                        { 
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se actualizaron {(numRegs < 0 ? 0 : numRegs)} registro(s)");
                            string idNombreProducto = $"El producto con Id: {txtId.Text} - Nombre de producto: {txtProducto.Text}:";
                            if (numRegs > 0)
                                U.NotificacionInformation(idNombreProducto + Utils.sms);
                            else if (numRegs == -1)
                                U.NotificacionError(idNombreProducto + Utils.nfmfe);
                            else if (numRegs == -2)
                                U.NotificacionError(idNombreProducto + Utils.nfmfm);
                            else
                                U.NotificacionError(idNombreProducto + Utils.nfmmd);
                            BorrarDatosProducto();
                            CargarValoresOriginales();
                        }
                        else
                        {
                            U.NotificacionError(resultado.mensaje);
                        }
                    }
                    catch (Exception ex)
                    {
                        U.NotificacionError("Error al modificar el producto: " + ex.Message);
                    }
                    finally
                    {
                        MDIPrincipal.ActualizarBarraDeEstado();
                    }
                }
            }
            else if (tabcOperacion.SelectedTab == tbpEliminar)
            {
                if (U.NotificacionQuestion($"[orange]¿Está seguro de eliminar el producto con Id: {txtId.Text} - Nombre de producto: {txtProducto.Text}?") == DialogResult.Yes)
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.eliminandoRegistro);
                    btnOperacion.Enabled = false;
                    Producto producto = new Producto
                    {
                        ProductID = int.Parse(txtId.Text),
                        RowVersion = (byte[])txtId.Tag
                    };
                    try
                    {
                        var resultado = await ApiProductoService.EliminarAsync(producto.ProductID, producto.RowVersion);
                        if (resultado.ok)
                        {
                            int numRegs = resultado.numRegs;
                            MDIPrincipal.ActualizarBarraDeEstado($"Se eliminaron {(numRegs < 0 ? 0 : numRegs)} registro(s)");
                            string idyNombre = $"El producto con Id: {txtId.Text} - Nombre de producto: {txtProducto.Text}:";
                            if (numRegs > 0)
                                U.NotificacionInformation(idyNombre + Utils.ses);
                            else if (numRegs == -1)
                                U.NotificacionError(idyNombre + Utils.nfefe);
                            else if (numRegs == -2)
                                U.NotificacionError(idyNombre + Utils.nfefm);
                            else
                                U.NotificacionError(idyNombre + Utils.nfemd);
                            BorrarDatosProducto();
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
                    btnOperacion.Enabled = false;
                }
            }
        }

        private void ActualizaDgv() => btnLimpiar.PerformClick();

        private void LlenarCombos()
        {
            LlenarCboCategoria();
            LlenarCboProveedor();
        }

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