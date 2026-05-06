using DAL;
using DAL.Helpers;
using Entities.DTOs;
using System.Data;

namespace BLL.Services
{
    public class EmpleadoService
    {
        private readonly ComboDataHelper _dal;
        private readonly EmpleadoDAL _empleadoDAL;

        public EmpleadoService(string connectionString)
        {
            _dal = new ComboDataHelper(connectionString);
            _empleadoDAL = new EmpleadoDAL(connectionString);
        }

        public DataTable ObtenerEmpleadosCbo()
        {
            var empleados = _dal.LlenarCbo("SpEmpleadoObtenerCbo");
            DataRow filaSeleccione = empleados.NewRow();
            filaSeleccione["EmployeeID"] = 0;
            filaSeleccione["EmployeeName"] = "»--- Seleccione ---«";
            empleados.Rows.InsertAt(filaSeleccione, 0);
            return empleados;
        }

        public DtoEmpleadosPaisesCbo ObtenerEmpleadoPais(int empleadoId)
        {
            return _empleadoDAL.ObtenerEmpleadoPais(empleadoId);
        }

        public DtoEmpleadoReportaACbo ObtenerEmpleadoReportaA(int empleadoId)
        {
            return _empleadoDAL.ObtenerEmpleadoReportaA(empleadoId);
        }
    }
}
