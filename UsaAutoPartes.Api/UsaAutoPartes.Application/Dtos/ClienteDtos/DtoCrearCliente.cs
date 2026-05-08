using System.ComponentModel.DataAnnotations;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.ClienteDtos
{
    public class DtoCrearCliente
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public Cliente Crear() => new Cliente(Nombre, Apellido, Telefono);
    }
}
