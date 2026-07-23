using System.Collections.Generic;
using UsaAutoPartes.Domain.Entities.BasesEntidades;

namespace UsaAutoPartes.Domain.Entities
{
    public class Marca : BaseEntity
    {
        public required string Nombre { get; set; }
        public string Prefijo { get; set; } = string.Empty;

        public List<Producto> Productos { get; set; } = new();
    }
}
