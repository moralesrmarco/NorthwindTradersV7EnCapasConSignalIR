using BLL.Services;
using Entities;
using Entities.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class MDIPrincipal : Form
    {
        private int childFormNumber = 0;
        public static MDIPrincipal Instance { get; private set; }
        public Usuario UsuarioLogueado = null;
        private HashSet<int> permisosUsuarioAutenticado = new HashSet<int>();

        private readonly string cnStr = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private readonly PermisoService _permisoService;

        public MDIPrincipal()
        {
            InitializeComponent();
            TabControlPrincipal.ConfigurarIconos(Properties.Resources.pestanaOff, Properties.Resources.pestanaOn);
            Instance = this;
            this.Text = Utils.nwtr;
            // Suscribirse al evento de Utils
            Utils.FormularioAgregado += (form) =>
            {
                ActualizarBarraDeEstado();
            };
            _permisoService = new PermisoService(cnStr);
        }

        public ToolStripStatusLabel ToolStripEstado
        {
            get { return toolStripStatus; }
            set { toolStripStatus = value; }
        }

        public static void ActualizarBarraDeEstado(string mensaje = "Listo.", bool error = false)
        {
            if (Instance != null && !Instance.IsDisposed)
            {
                if (mensaje != "Listo.")
                {
                    if (error)
                        Instance.ToolStripEstado.BackColor = System.Drawing.Color.Red;
                    else
                        Instance.ToolStripEstado.BackColor = SystemColors.ActiveCaption;
                }
                else
                {
                    if (error)
                    {
                        Instance.ToolStripEstado.ForeColor = System.Drawing.Color.White;
                        Instance.ToolStripEstado.Font = new Font(Instance.ToolStripEstado.Font, FontStyle.Bold);
                    }
                    else
                    {
                        Instance.ToolStripEstado.ForeColor = SystemColors.ControlText;
                        Instance.ToolStripEstado.BackColor = SystemColors.Control;
                        Instance.ToolStripEstado.Font = new Font(Instance.ToolStripEstado.Font, FontStyle.Regular);
                    }
                }
                Instance.ToolStripEstado.Text = mensaje;
                Instance.Refresh();
            }
        }

        private void MDIPrincipal_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            toolStripStatusLabel2.Text = UsuarioLogueado.User;
            ConfiguracionFiscal.TasaIVA = Convert.ToDecimal(ConfigurationManager.AppSettings["TasaIVA"]);
            ActualizarBarraDeEstado("Sesión iniciada correctamente.     |     Bienvenido " + UsuarioLogueado.NombreCompleto + " al sistema " + Utils.nwtr.Substring(2, (Utils.nwtr.Length - 4)) + ". Para comenzar, seleccione una opción del menú correspondiente a sus permisos de usuario.");
            IniciarSesion();
            //if (permisosUsuarioAutenticado.Contains(10))
            //{
            //    FrmTableroControlAltaDireccion frm = new FrmTableroControlAltaDireccion();
            //    Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Tablero de control para la alta dirección «");
            //}
            //else if (permisosUsuarioAutenticado.Contains(12))
            //{
            //    FrmTableroControlVendedores frm = new FrmTableroControlVendedores();
            //    Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Tablero de control para los vendedores «");
            //}
            ActualizarBarraDeEstado("Sesión iniciada correctamente.     |     Bienvenido " + UsuarioLogueado.NombreCompleto + " al sistema " + Utils.nwtr.Substring(2, (Utils.nwtr.Length - 4)) + ". Para comenzar, seleccione una opción del menú correspondiente a sus permisos de usuario."); // Se repite para asegurar que se muestre después de cargar los tableros de control, si el usuario tiene permisos para ellos.
        }

        private void TabControlPrincipal_SelectedIndexChanged(object sender, EventArgs e) => MDIPrincipal.ActualizarBarraDeEstado();

        private void IniciarSesion()
        {
            // Obtener los permisos del usuario logueado
            permisosUsuarioAutenticado = _permisoService.ObtenerPermisosPorUsuarioId(UsuarioLogueado.Id);
            // Ajustar el menú por permisos
            AjustarMenuPorPermisos(permisosUsuarioAutenticado);
            if (permisosUsuarioAutenticado.Count == 0)
            {
                U.NotificacionWarning("El usuario no tiene permisos asignados.");
            }
        }

        private void AjustarMenuPorPermisos(HashSet<int> permisos)
        {
            empleadosToolStripMenuItem.Enabled = false;
            clientesToolStripMenuItem.Enabled = false;
            proveedoresToolStripMenuItem.Enabled = false;
            categoríasToolStripMenuItem.Enabled = false;
            productosToolStripMenuItem.Enabled = false;
            ventasToolStripMenuItem.Enabled = false;
            administraciónToolStripMenuItem.Enabled = false;
            gráficasToolStripMenuItem.Enabled = false;
            tablerosDeControlToolStripMenuItem.Enabled = false;
            tableroDeControlParaLaAltaDirecciónToolStripMenuItem.Enabled = false;
            tableroDeControlParaLosVendedoresToolStripMenuItem.Enabled = false;
            foreach (int permisoId in permisos)
            {
                if (permisoId == 1)
                    empleadosToolStripMenuItem.Enabled = true; // Permiso para Empleados
                else if (permisoId == 2)
                    clientesToolStripMenuItem.Enabled = true; // Permiso para Clientes
                else if (permisoId == 3)
                    proveedoresToolStripMenuItem.Enabled = true; // Permiso para Proveedores
                else if (permisoId == 4)
                    categoríasToolStripMenuItem.Enabled = true; // Permiso para Categorías
                else if (permisoId == 5)
                    productosToolStripMenuItem.Enabled = true; // Permiso para Productos
                else if (permisoId == 6)
                    ventasToolStripMenuItem.Enabled = true; // Permiso para Pedidos
                else if (permisoId == 7)
                    administraciónToolStripMenuItem.Enabled = true; // Permiso para Administración
                else if (permisoId == 8)
                    gráficasToolStripMenuItem.Enabled = true;
                else if (permisoId == 10 || permisoId == 12)
                {
                    tablerosDeControlToolStripMenuItem.Enabled = true; // Permiso para Tableros de control
                    if (permisoId == 10)
                        tableroDeControlParaLaAltaDirecciónToolStripMenuItem.Enabled = true; // Permiso para Tablero de control para la alta dirección
                    else if (permisoId == 12)
                        tableroDeControlParaLosVendedoresToolStripMenuItem.Enabled = true; // Permiso para Tablero de control para los vendedores
                }
            }
        }

        private void cerrarTodasLasPestañasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.CerrarTodasLasPestañas(TabControlPrincipal);
        }

        private void cerrarLaPestañaSeleccionadaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.CerrarPestañaSeleccionada(TabControlPrincipal);
        }

        private void ShowNewForm(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MdiParent = this;
            childForm.Text = "Ventana " + childFormNumber++;
            childForm.Show();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void cambiarContraseñaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FrmCambiarContrasena frm = new FrmCambiarContrasena())
            {
                frm.UsuarioLogueado = toolStripStatusLabel2.Text;
                frm.ShowDialog();
            }
        }

        private void cambiarDeUsuarioLogueadoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Copiamos la colección a una lista independiente
            var formularios = Application.OpenForms.Cast<Form>().ToList();

            foreach (Form frm in formularios)
            {
                if (frm is FrmEmpleadosCrud empleadosForm)
                {
                    if (Utils.HayCambios(empleadosForm, empleadosForm.valoresOriginales, empleadosForm.errorProvider1))
                    {
                        // Selecciona la pestaña que contiene el formulario
                        foreach (TabPage page in TabControlPrincipal.TabPages)
                        {
                            if (page.Controls.Contains(empleadosForm))
                            {
                                TabControlPrincipal.SelectedTab = page;
                                empleadosForm.Focus(); // ahora sí se verá el ErrorProvider
                                break;
                            }
                        }
                        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en empleados. ¿Desea reiniciar de todas formas?");
                        if (result == DialogResult.No)
                            return; // Cancela el reinicio
                    }
                    empleadosForm.FormClosing -= empleadosForm.FrmEmpleadosCrud_FormClosing;
                }
                //if (frm is FrmClientesCrud clientesForm)
                //{
                //    if (Utils.HayCambios(clientesForm, clientesForm.valoresOriginales, clientesForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(clientesForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                clientesForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en clientes. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    clientesForm.FormClosing -= clientesForm.FrmClientesCrud_FormClosing;
                //}
                //if (frm is FrmProveedoresCrud proveedoresForm)
                //{
                //    if (Utils.HayCambios(proveedoresForm, proveedoresForm.valoresOriginales, proveedoresForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(proveedoresForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                proveedoresForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en proveedores. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    proveedoresForm.FormClosing -= proveedoresForm.FrmProveedoresCrud_FormClosing;
                //}
                //if (frm is FrmCategoriasCrud categoriasForm)
                //{
                //    if (Utils.HayCambios(categoriasForm, categoriasForm.valoresOriginales, categoriasForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(categoriasForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                categoriasForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en categorias. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    categoriasForm.FormClosing -= categoriasForm.FrmCategoriasCrud_FormClosing;
                //}
                //if (frm is FrmProductosCrud productosForm)
                //{
                //    if (Utils.HayCambios(productosForm, productosForm.valoresOriginales, productosForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(productosForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                productosForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en productos. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    productosForm.FormClosing -= productosForm.FrmProductosCrud_FormClosing;
                //}
                //if (frm is FrmVentasCrud ventasForm)
                //{
                //    if (Utils.HayCambios(ventasForm, ventasForm.valoresOriginales, ventasForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(ventasForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                ventasForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en ventas. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    ventasForm.FormClosing -= ventasForm.FrmVentasCrud_FormClosing;
                //}
                //if (frm is FrmVentasCrudV2 ventasFormV2)
                //{
                //    if (Utils.HayCambios(ventasFormV2, ventasFormV2.valoresOriginales, ventasFormV2.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(ventasFormV2))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                ventasFormV2.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en ventas. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    ventasFormV2.FormClosing -= ventasFormV2.FrmVentasCrud_FormClosing;
                //}
                //if (frm is FrmVentasDetalleCrud ventasDetalleForm)
                //{
                //    if (Utils.HayCambios(ventasDetalleForm, ventasDetalleForm.valoresOriginales, ventasDetalleForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(ventasDetalleForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                ventasDetalleForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en ventas detalle. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    ventasDetalleForm.FormClosing -= ventasDetalleForm.FrmVentasDetalleCrud_FormClosing;
                //}
                //if (frm is FrmUsuariosCrud usuariosCrudForm)
                //{
                //    if (Utils.HayCambios(usuariosCrudForm, usuariosCrudForm.valoresOriginales, usuariosCrudForm.errorProvider1))
                //    {
                //        foreach (TabPage page in TabControlPrincipal.TabPages)
                //        {
                //            if (page.Controls.Contains(usuariosCrudForm))
                //            {
                //                TabControlPrincipal.SelectedTab = page;
                //                usuariosCrudForm.Focus();
                //                break;
                //            }
                //        }
                //        var result = U.NotificacionQuestion("[orange]Hay cambios sin guardar en usuarios. ¿Desea reiniciar de todas formas?");
                //        if (result == DialogResult.No)
                //            return;
                //    }
                //    usuariosCrudForm.FormClosing -= usuariosCrudForm.FrmUsuariosCrud_FormClosing;
                //}
            }
            // Reinicia la aplicación
            Application.Restart();
        }

        private void mantenimientoDeEmpleadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmEmpleadosCrud frm = new FrmEmpleadosCrud();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Mantenimiento de empleados «");
        }

        private void reporteDeEmpleadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmRptEmpleados frm = new FrmRptEmpleados();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Reporte de empleados «");
        }

        private void reporteDeEmpleadosConFotoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmRptEmpleadosConFoto frm = new FrmRptEmpleadosConFoto();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Reporte de empleados con foto «");
        }

        private void reporteDeEmpleadosConFoto2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmRptEmpleado2 frm = new FrmRptEmpleado2();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Reporte de empleados con foto 2 «");
        }

        private void mantenimientoDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmClientesCrud frm = new FrmClientesCrud();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Mantenimiento de clientes «");
        }

        private void directorioDeClientesYProveedoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmClientesyProveedoresDirectorio frm = new FrmClientesyProveedoresDirectorio();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Directorio de clientes y proveedores «");
        }

        private void directorioDeClientesYProveedoresPorCiudadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmClientesyProveedoresDirectorioxCiudad frm = new FrmClientesyProveedoresDirectorioxCiudad();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Directorio de clientes y proveedores por ciudad «");
        }

        private void directorioDeClientesYProveedoresPorPaísToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmClientesyProveedoresDirectorioxPais frm = new FrmClientesyProveedoresDirectorioxPais();
            Utils.AgregarFormularioEnTab(TabControlPrincipal, frm, "» Directorio de clientes y proveedores por país «");
        }

        private void directorioDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void directorioDeClientesYProveedoresToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void directorioDeClientesYProveedoresPorCiudadToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void directorioDeClientesYProveedoresPorPaísToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }
    }
}
