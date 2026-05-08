using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigPiezaKit : IEntityTypeConfiguration<PiezaKit>
    {
        public void Configure(EntityTypeBuilder<PiezaKit> builder)
        {
            builder.ToTable("PiezaKit");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id_Producto).IsRequired();

            builder.Property(x => x.CodigoUniversal).IsRequired();

            builder.Property(x => x.Nombre).IsRequired();

            builder.Property(x => x.CantidadPorKit).IsRequired();

            builder.Property(x => x.StockActual).IsRequired();

            builder.HasOne(x => x.Producto)
                .WithMany(x => x.PiezasKit)
                .HasForeignKey(x => x.Id_Producto)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_PiezaKit_Producto");

            builder.HasIndex(x => x.Id_Producto);
            builder.HasIndex(x => x.CodigoUniversal);
        }
    }
}
