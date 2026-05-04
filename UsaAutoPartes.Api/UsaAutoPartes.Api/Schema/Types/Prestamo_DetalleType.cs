using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class Prestamo_DetalleType : ObjectType<Prestamo_detalle>
    {
        protected override void Configure(IObjectTypeDescriptor<Prestamo_detalle> builder)
        {
            base.Configure(builder);
        }
    }
}
