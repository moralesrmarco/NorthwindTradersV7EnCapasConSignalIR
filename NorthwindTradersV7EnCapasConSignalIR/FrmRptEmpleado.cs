using BLL;
using Entities;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmRptEmpleado : Form
    {

        public int Id { get; set; }
        string _connectionString = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private EmpleadoBLL _empleadoBLL;

        public FrmRptEmpleado()
        {
            InitializeComponent();
            reportViewer1.BackColor = SystemColors.GradientInactiveCaption;
            _empleadoBLL = new EmpleadoBLL(_connectionString);
        }

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmRptEmpleado_FormClosed(object sender, FormClosedEventArgs e) => MDIPrincipal.ActualizarBarraDeEstado();

        private void FrmRptEmpleado_Load(object sender, EventArgs e)
        {
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                var empleado = _empleadoBLL.ObtenerEmpleadoPorId(Id);
                Empleado.NormalizarFotos(new List<Empleado> { empleado }, Utilities.Utils.StripOleHeader);
                if (empleado != null) 
                    MDIPrincipal.ActualizarBarraDeEstado($"Se encontró el registro con el Id: {empleado.EmployeeID}");
                // Crear una lista con un solo empleado
                List<Empleado> empleados = new List<Empleado> { empleado };
                // Asignar la lista como fuente de datos del informe
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", empleados));
                reportViewer1.BackColor = Color.White;
                reportViewer1.RefreshReport();
                if (empleado == null)
                {
                    MDIPrincipal.ActualizarBarraDeEstado(Utils.noDatos);
                    U.NotificacionWarning(Utils.noDatos);
                }
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
        }
    }
}
