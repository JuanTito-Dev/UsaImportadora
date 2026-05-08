using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigOrdenVentaItem : IEntityTypeConfiguration<OrdenVentaItem>
    {
        public void Configure(EntityTypeBuilder<OrdenVentaItem> builder)
        {
            builder.ToTable("OrdenVentaItem");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Estado).IsRequired().HasMaxLength(20);

            builder.Property(x => x.PrecioUnitario).HasPrecision(10, 2);

            builder.Property(x => x.MontoDescuento).HasPrecision(10, 2);

            builder.HasOne(x => x.Orden)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.Id_Orden)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrdenVentaItem_Orden");

            builder.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVentaItem_Producto");

            builder.HasOne(x => x.Descuento)
                .WithMany()
                .HasForeignKey(x => x.Id_Descuento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVentaItem_Descuento");
        }
    }
}
