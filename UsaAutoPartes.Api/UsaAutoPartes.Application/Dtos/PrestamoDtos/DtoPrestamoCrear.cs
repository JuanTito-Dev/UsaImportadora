using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.PrestamoDtos
{
    public class DtoPrestamoCrear
    {
        [Required(ErrorMessage = "Cliente requerido")]
        public int Id_Cliente { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        public string Nota { get; set; } = string.Empty;

        public List<DtoPrestamoDetalleCrear> Detalles { get; set; } = new List<DtoPrestamoDetalleCrear>();

        public Prestamo Crear(string nombre)
        {
            return new Prestamo(Id_Cliente, nombre, Fecha, Nota);
        }
    }
}
