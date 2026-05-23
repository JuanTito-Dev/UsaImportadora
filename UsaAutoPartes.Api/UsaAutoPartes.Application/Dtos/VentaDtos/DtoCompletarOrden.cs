using System.ComponentModel.DataAnnotations;

namespace UsaAutoPartes.Application.Dtos.VentaDtos
{
    public class DtoCompletarOrden
    {
        [Required]
        [MinLength(1)]
        public List<DtoPago> Pagos { get; set; } = new();
    }
}
