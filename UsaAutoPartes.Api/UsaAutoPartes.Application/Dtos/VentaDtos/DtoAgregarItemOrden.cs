using System.ComponentModel.DataAnnotations;

namespace UsaAutoPartes.Application.Dtos.VentaDtos
{
    public class DtoAgregarItemOrden
    {
        [Required]
        public int Id_Producto { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Cantidad { get; set; }
    }
}
