using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigOrdenVenta : IEntityTypeConfiguration<OrdenVenta>
    {
        public void Configure(EntityTypeBuilder<OrdenVenta> builder)
        {
            builder.ToTable("OrdenVenta");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Estado).IsRequired().HasMaxLength(20);

            builder.Property(x => x.Fecha).IsRequired();

            builder.Property(x => x.FechaEsperandoPago).IsRequired(false);

            builder.HasIndex(x => x.Estado).HasDatabaseName("IX_OrdenVenta_Estado");

            builder.HasIndex(x => x.Id_Cajero).HasDatabaseName("IX_OrdenVenta_Cajero");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Id_Cliente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVenta_Cliente");

            builder.HasOne(x => x.Caja)
                .WithMany()
                .HasForeignKey(x => x.Id_Caja)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVenta_Caja");

            builder.HasOne(x => x.Cajero)
                .WithMany()
                .HasForeignKey(x => x.Id_Cajero)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVenta_Cajero");

            builder.HasOne(x => x.Almacenero)
                .WithMany()
                .HasForeignKey(x => x.Id_Almacenero)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenVenta_Almacenero");
        }
    }
}
