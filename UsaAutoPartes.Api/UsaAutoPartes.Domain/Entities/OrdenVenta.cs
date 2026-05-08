using UsaAutoPartes.Domain.Entities.BasesEntidades;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Domain.Enum.VentaEnums;

namespace UsaAutoPartes.Domain.Entities
{
    public class OrdenVenta : BaseEntity
    {
        public Guid Id_Cajero { get; set; }
        public Guid? Id_Almacenero { get; set; }
        public int? Id_Cliente { get; set; }
        public int Id_Caja { get; set; }
        public string Estado { get; set; } = EstadosOrden.Pendiente;
        public DateTime Fecha { get; set; }
        public DateTime? FechaCompletada { get; set; }
        public string? NotaCancelacion { get; set; }

        public Usuario? Cajero { get; set; }
        public Usuario? Almacenero { get; set; }
        public Cliente? Cliente { get; set; }
        public Caja? Caja { get; set; }
        public List<OrdenVentaItem> Items { get; set; } = new();

        public OrdenVenta() { }

        public OrdenVenta(Guid idCajero, int idCaja, int? idCliente)
        {
            Id_Cajero = idCajero;
            Id_Caja = idCaja;
            Id_Cliente = idCliente;
            Fecha = DateTime.UtcNow;
        }

        public void Aceptar(Guid idAlmacenero)
        {
            Id_Almacenero = idAlmacenero;
            Estado = EstadosOrden.Aceptada;
        }

        public void MarcarLista()
        {
            Estado = EstadosOrden.Lista;
        }

        public void Completar()
        {
            Estado = EstadosOrden.Completada;
            FechaCompletada = DateTime.UtcNow;
        }

        public void Cancelar(string? nota)
        {
            Estado = EstadosOrden.Cancelada;
            NotaCancelacion = nota;
        }
    }
}
