using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public class DtoListaProducto
    {
        [Required(ErrorMessage = "El campo ConversionABs es obligatorio.")]
        public decimal ConversionABs { get; set; }

        public List<DtoProductosLista> Productos { get; set; } = new List<DtoProductosLista>();
    }
}
