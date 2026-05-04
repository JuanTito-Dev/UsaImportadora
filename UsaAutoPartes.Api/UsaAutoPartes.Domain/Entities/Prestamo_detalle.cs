using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities.BasesEntidades;

namespace UsaAutoPartes.Domain.Entities
{
    public class Prestamo_detalle : BaseEntity
    { 
        public int Id_Prestamo { get; set; }
        public required string Codigo { get; set; } 

        public string Nombre { get; set; } = string.Empty;

        public int Cantidad { get; set; } = 1;

        public required decimal Precio { get; set; }

        public Prestamo? Prestamo { get; set; }

        public Prestamo_detalle() { }

        public Prestamo_detalle(string codigo, string nombre, int cantidad, decimal precio)
        {
            Codigo = codigo;
            Nombre = nombre;
            Cantidad = cantidad;
            Precio = precio;
        }

        public decimal Total()
        {
            return Cantidad * Precio;
        }
    }
}
