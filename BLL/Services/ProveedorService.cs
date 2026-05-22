using DAL;
using DAL.Helpers;
using System.Data;

namespace BLL.Services
{
    public class ProveedorService
    {
        private readonly ComboDataHelper _dal;
        private readonly ProveedorDAL _proveedorDAL;

        public ProveedorService(string connectionString)
        {
            _dal = new ComboDataHelper(connectionString);
            _proveedorDAL = new ProveedorDAL(connectionString);
        }

        public DataTable ObtenerProveedoresCbo()
        {
            var proveedores = _dal.LlenarCbo("SpProveedorObtenerCbo");
            DataRow filaSeleccione = proveedores.NewRow();
            filaSeleccione["SupplierID"] = 0;
            filaSeleccione["CompanyName"] = "»--- Seleccione ---«";
            proveedores.Rows.InsertAt(filaSeleccione, 0);
            return proveedores;
        }

        public string ObtenerProveedorPais(int proveedorId)
        {
            return _proveedorDAL.ObtenerProveedorPais(proveedorId);
        }
    }
}
