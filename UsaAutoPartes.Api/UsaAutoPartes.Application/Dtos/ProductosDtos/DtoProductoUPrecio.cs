using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public class DtoProductoUPrecio
    {
        [Required(ErrorMessage = "Costo es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Costo debe ser mayor a 0")]
        public decimal Costo { get; set; }

        [Required(ErrorMessage = "Precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "ConversionABs es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "ConversionABs debe ser mayor a 0")]
        public decimal ConversionABs { get; set; }

        public string Nota { get; set; } = string.Empty;
    }
}
