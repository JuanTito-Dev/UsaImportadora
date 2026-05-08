using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class PiezaKitType : ObjectType<PiezaKit>
    {
        protected override void Configure(IObjectTypeDescriptor<PiezaKit> descriptor)
        {
            descriptor.Ignore(x => x.Producto);
        }
    }
}
