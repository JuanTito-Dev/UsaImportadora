using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class ProductoType : ObjectType<Producto>
    {
        protected override void Configure(IObjectTypeDescriptor<Producto> producto)
        {
            base.Configure(producto);
            producto.Field(p => p.Id).Type<NonNullType<IdType>>();
            producto.Field(p => p.Codigo).Type<NonNullType<StringType>>();
            producto.Field(p => p.Nombre).Type<NonNullType<StringType>>();
            producto.Field(p => p.MarcaId).Type<IntType>();
            producto.Field(p => p.Procedencia).Type<StringType>();
            producto.Field(P => P.HistorialPrecios).Type<ListType<HistorialPrecioType>>();
            producto.Field(P => P.PiezasKit).Type<ListType<PiezaKitType>>();
            producto.Field("calcularStockKit")
                .Type<IntType>()
                .Description("Stock total (raw) del kit, sin descontar reservas de piezas. Útil para inventario físico.")
                .Resolve(ctx => ctx.Parent<Producto>().CalcularStockKit());

            producto.Field("calcularStockKitDisponible")
                .Type<IntType>()
                .Description("Stock disponible del kit, descontando las piezas reservadas por otras órdenes. Es el número que se muestra en la lista de búsqueda del cajero.")
                .Resolve(ctx => ctx.Parent<Producto>().CalcularStockKitDisponible());
        }
    }
}
