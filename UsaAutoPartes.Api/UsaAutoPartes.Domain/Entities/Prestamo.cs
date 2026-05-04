using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities.BasesEntidades;
using UsaAutoPartes.Domain.Enum.InventarioEnums;

namespace UsaAutoPartes.Domain.Entities
{
    public class Prestamo : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Nota { get; set;  } = string.Empty;

        public decimal Total { get; private set; } = 0.00M;

        public string Estado { get; private set; } = EstadosPrestamo.Activo;

        public List<Prestamo_detalle> Detalle { get; set; } = new List<Prestamo_detalle>();

        public Prestamo() { }

        public Prestamo(string nombre, DateTime fecha, string nota)
        {
            Nombre = nombre;
            Fecha = fecha;
            Nota = nota;
        }

        public void CancelarPedido()
        {
            Estado = EstadosPrestamo.Cancelado;
        }

        public void SumarPrecio(decimal precio)
        {
            Total += precio;
        }
    }
}
