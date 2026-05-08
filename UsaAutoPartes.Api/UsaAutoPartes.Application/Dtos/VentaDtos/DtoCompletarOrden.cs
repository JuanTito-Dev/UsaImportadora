using System.ComponentModel.DataAnnotations;

namespace UsaAutoPartes.Application.Dtos.VentaDtos
{
    public class DtoCompletarOrden
    {
        [Required]
        public string TipoPago { get; set; } = string.Empty;
    }
}
