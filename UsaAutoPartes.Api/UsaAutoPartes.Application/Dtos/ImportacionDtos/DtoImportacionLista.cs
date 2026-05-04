using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ImportacionDtos
{
    public class DtoImportacionLista
    {
        [Required]
        public required int Id_Proveedor { get; set; }

        [Required]
        public required DateTime Fecha { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Costo total debe ser mayor a 0")]
        public required decimal CostoTotal {  get; set; }

        [Required]
        public decimal F_Internacional { get; set; } = 0.00M;

        [Required]
        public decimal Aduana_Arancel { get; set; } = 0.00M;

        [Required]
        public decimal Trasporte_Interno { get; set; } = 0.00M;

        public List<DtoImportacionProducto> Productos { get; set; } = new List<DtoImportacionProducto>();
    }
}
