using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Schema.Types
{
    public class OrdenVentaType : ObjectType<OrdenVenta>
    {
        protected override void Configure(IObjectTypeDescriptor<OrdenVenta> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Ignore(x => x.Cajero);
            descriptor.Ignore(x => x.Almacenero);
            descriptor.Ignore(x => x.Caja);
            descriptor.Field(x => x.Items).Type<ListType<OrdenVentaItemType>>();
        }
    }
}
