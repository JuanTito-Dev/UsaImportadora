using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.Dtos.ProductosDtos;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.Dtos.ImportacionDtos
{
    public class DtoImportacionProducto : DtoProductosLista
    {
        public override Producto Crear(decimal conversionABs)
        {
            var producto = base.Crear(conversionABs);
            return producto;
        }

        public override HistorialPrecio Actualizar(Producto producto, decimal conversionABs, string Nota)
        {
            return base.Actualizar(producto, conversionABs, Nota);
        }

        public Importacion_Detalle CrearImportacionDetalle(decimal conversionABs)
        {
            var detalle = new Importacion_Detalle
            {
                Codigo = this.Codigo,
                CodigoAux = this.CodigoAux,
                CodigoAux2 = this.CodigoAux2,
                Nombre = this.Nombre,
                Marca = this.Marca,
                Descripcion = this.Descripcion,
                Unidad_Medida = this.Unidad_Medida,
                Ubicacion = this.Ubicacion,
                Stock_Actual = this.Stock_Actual,
                Stock_Minimo = this.Stock_Minimo,
                Costo = this.Costo,
                Precio = this.Precio,
                ConversionABs = conversionABs
            };
            return detalle;
        }
    }
}
