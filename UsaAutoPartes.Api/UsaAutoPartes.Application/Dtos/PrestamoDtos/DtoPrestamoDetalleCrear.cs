using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.PrestamoDtos
{
    public class DtoPrestamoDetalleCrear
    {
        [Required(ErrorMessage = "Codigo necesario")]
        public required string Codigo { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Rango invalido")]
        public int Cantidad { get; set; } 

        public Prestamo_detalle Crear(string Nombre, decimal precio)
        {
            return new Prestamo_detalle
            {
                Codigo = this.Codigo,
                Nombre = Nombre,
                Precio = precio,
                Cantidad = Cantidad
            };
        }
    }
}
