using System.ComponentModel.DataAnnotations;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.ProductosDtos
{
    public class DtoPiezaKit
    {
        public string CodigoUniversal { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int CantidadPorKit { get; set; } = 1;

        public PiezaKit Crear()
        {
            var codigoBase = string.IsNullOrWhiteSpace(CodigoUniversal)
                ? Guid.NewGuid().ToString("N")[..8].ToUpper()
                : CodigoUniversal;

            return new PiezaKit(codigoBase, Nombre, CantidadPorKit);
        }
    }
}
