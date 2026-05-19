using DAL;
using DAL.Helpers;
using System.Data;

namespace BLL.Services
{
    public class ClienteService
    {
        private readonly ComboDataHelper _dal;
        private readonly ClienteDAL _clienteDAL;

        public ClienteService(string connectionString)
        {
            _dal = new ComboDataHelper (connectionString);
            _clienteDAL = new ClienteDAL(connectionString);
        }

        public DataTable ObtenerClientesCbo()
        {
            var clientes = _dal.LlenarCbo("SpClienteObtenerCbo");
            DataRow filaSeleccione = clientes.NewRow();
            filaSeleccione["CustomerID"] = 0;
            filaSeleccione["CompanyName"] = "»--- Seleccione ---«";
            clientes.Rows.InsertAt(filaSeleccione, 0);
            return clientes;
        }

        public string ObtenerClientePais(string clienteId)
        {
            return _clienteDAL.ObtenerClientePais(clienteId);
        }
    }
}
