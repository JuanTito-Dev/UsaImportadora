using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class PrestamoType : ObjectType<Prestamo>
    {
        protected override void Configure(IObjectTypeDescriptor<Prestamo> builder)
        {
            base.Configure(builder);

            builder.Field(x => x.Detalle).Type<ListType<Prestamo_DetalleType>>();
        }
    }
}
