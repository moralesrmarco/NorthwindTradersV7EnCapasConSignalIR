using System;
using System.Collections.Generic;

namespace Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Paterno { get; set; }
        public string Materno { get; set; }
        public string Nombres { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public DateTime FechaCaptura { get; set; }
        public DateTime FechaModificacion { get; set; }
        public bool Estatus { get; set; }
        public byte[] RowVersion { get; set; }

        public HashSet<int> PermisosIds { get; set; }

        public string RowVersionStr
        {
            get
            {
                if (RowVersion == null || RowVersion.Length < 8)
                    return string.Empty;

                return BitConverter.ToInt64(RowVersion, 0).ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    RowVersion = null;
                    return;
                }

                RowVersion = BitConverter.GetBytes(long.Parse(value));
            }
        }

        public string NombreCompleto => $"{Paterno} {Materno} {Nombres}".Trim();
        // aqui debo poner los objetos de navegación, es decir las propiedades que representan las relaciones con otras entidades como es permisos
    }
}
