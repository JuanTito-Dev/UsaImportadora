using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities.BasesEntidades;

namespace UsaAutoPartes.Domain.Entities
{
    public  class Producto : BaseEntity 
    {
        public required string Codigo { get; set; }

        public string CodigoAux { get; set; } = string.Empty;

        public string CodigoAux2 { get; set; } = string.Empty;

        public required string Nombre { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string Unidad_Medida { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public required int Stock_Actual { get; set; }

        public required int Stock_Minimo { get; set; }

        public decimal Costo { get; private set; }

        public decimal Precio { get; private set; }

        public decimal ConversionABs { get; private set; }

        public List<HistorialPrecio> HistorialPrecios { get; set; } = new List<HistorialPrecio>();


        public Producto() { }
        public Producto(decimal costo, decimal precio, decimal conversionABs)
        {
            Costo = costo;
            Precio = precio;
            ConversionABs = conversionABs;
            HistorialPrecios.Add(new HistorialPrecio
            {
                Id_producto = this.Id,
                Costo = costo,
                Precio = precio,
                ConversionABs = conversionABs,
                Nota = "Creación del producto"
            });
        }

        public HistorialPrecio CambiarPrecio(decimal Costo, decimal venta, decimal ConversionABs, string Nota)
        {
            this.Costo = Costo;
            this.Precio = venta;
            this.ConversionABs = ConversionABs;

            return new HistorialPrecio
            {
                Id_producto = this.Id,
                Costo = Costo,
                Precio = venta,
                ConversionABs = ConversionABs,
                Nota = Nota
            };
        }
    }
}
