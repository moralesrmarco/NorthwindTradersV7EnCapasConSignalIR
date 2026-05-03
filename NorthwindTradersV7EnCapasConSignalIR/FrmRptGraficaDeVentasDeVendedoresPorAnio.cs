using BLL.Services;
using Microsoft.Reporting.WinForms;
using System;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using Utilities;

namespace NorthwindTradersV7EnCapasConSignalIR
{
    public partial class FrmRptGraficaDeVentasDeVendedoresPorAnio : Form
    {
        private readonly string cnStr = ConfigurationManager.ConnectionStrings["Northwind2ConnectionString"].ConnectionString;
        private readonly GraficasService _graficasService;
        
        public FrmRptGraficaDeVentasDeVendedoresPorAnio()
        {
            InitializeComponent();
            _graficasService = new GraficasService(cnStr);
        }

        private void GrbPaint(object sender, PaintEventArgs e) => Utils.GrbPaint(this, sender, e);

        private void FrmRptGraficaDeVentasDeVendedoresPorAnio_Load(object sender, EventArgs e)
        {
            LlenarCmbVentas();
        }

        private void LlenarCmbVentas()
        {
            try
            {
                CmbVentas.SelectedIndexChanged -= CmbVentas_SelectedIndexChanged;
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                var dt = _graficasService.ObtenerTop10AñosDeVentas();
                CmbVentas.DataSource = dt;
                CmbVentas.DisplayMember = "Texto";
                CmbVentas.ValueMember = "Valor";
                CmbVentas.SelectedIndexChanged += CmbVentas_SelectedIndexChanged;
                CmbVentas.SelectedValue = DateTime.Today.Year;
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

        private void CmbVentas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CmbVentas.SelectedIndex == 0)
            {
                MessageBox.Show("Seleccione un año válido.");
                return;
            }
            LlenarGrafico(Convert.ToInt32(CmbVentas.SelectedValue.ToString()));
        }

        private void LlenarGrafico(int year)
        {
            string tit = string.Empty;
            if (year > 0) 
                tit = $"» Reporte gráfico de ventas por vendedores ({year}) «";
            else
                tit = "» Reporte gráfico de ventas por vendedores (todos los años) «";
            string subTit = string.Empty;
            if (year > 0)
                subTit = $"Ventas por vendedores ({year})";
            else
                subTit = "Ventas por vendedores (todos los años)";
            groupBox1.Text = tit;
            DataTable dt = null;
            try
            {
                MDIPrincipal.ActualizarBarraDeEstado(Utils.clbdd);
                dt = ReportDataTableAdapterHelper.ConvertirVendedorTotalVentas(_graficasService.ObtenerVentasPorVendedores(year));
            }
            catch (Exception ex)
            {
                U.MsgCatchOue(ex);
            }
            finally
            {
                MDIPrincipal.ActualizarBarraDeEstado();
            }
            reportViewer1.LocalReport.DataSources.Clear();
            var rds = new ReportDataSource("DataSet1", dt);
            reportViewer1.LocalReport.DataSources.Add(rds);
            reportViewer1.LocalReport.SetParameters(new ReportParameter("Subtitulo", subTit));
            reportViewer1.LocalReport.SetParameters(new ReportParameter("Anio", year.ToString()));
            reportViewer1.RefreshReport();
        }
    }
}
