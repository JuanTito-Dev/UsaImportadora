using System.ComponentModel.DataAnnotations;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.VentaDtos
{
    public class DtoCrearOrden
    {
        public int? Id_Cliente { get; set; }

        [Required]
        [MinLength(1)]
        public List<DtoItemOrden> Items { get; set; } = new();

        public OrdenVenta Crear(Guid idCajero, int idCaja)
        {
            var orden = new OrdenVenta(idCajero, idCaja, Id_Cliente);
            orden.Items = Items.Select(i => i.Crear()).ToList();
            return orden;
        }
    }
}
