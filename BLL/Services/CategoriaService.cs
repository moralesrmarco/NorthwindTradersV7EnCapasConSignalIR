using DAL;
using DAL.Helpers;
using Entities;
using System.Data;

namespace BLL.Services
{
    public class CategoriaService
    {
        private readonly ComboDataHelper _dal;
        private readonly CategoriaDAL _categoriaDAL;

        public CategoriaService(string connectionString)
        {
            _dal = new ComboDataHelper(connectionString);
            _categoriaDAL = new CategoriaDAL(connectionString);
        }

        public DataTable ObtenerCategoriasCbo()
        {
            var categorias = _dal.LlenarCbo("SpCategoriaObtenerCbo");
            DataRow filaSeleccione = categorias.NewRow();
            filaSeleccione["CategoryID"] = 0;
            filaSeleccione["CategoryName"] = "»--- Seleccione ---«";
            categorias.Rows.InsertAt(filaSeleccione, 0);
            return categorias;
        }
    }
}
