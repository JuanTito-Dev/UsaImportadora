using System.ComponentModel.DataAnnotations;

namespace UsaAutoPartes.Application.Dtos.AjusteStockDtos
{
    public class DtoAjusteStock
    {
        [Required(ErrorMessage = "NuevaCantidad requerida")]
        [Range(0, int.MaxValue, ErrorMessage = "NuevaCantidad debe ser >= 0")]
        public required int NuevaCantidad { get; set; }

        [Required(ErrorMessage = "Motivo requerido")]
        [MaxLength(200)]
        public required string Motivo { get; set; }

        [MaxLength(500)]
        public string Nota { get; set; } = string.Empty;
    }
}
