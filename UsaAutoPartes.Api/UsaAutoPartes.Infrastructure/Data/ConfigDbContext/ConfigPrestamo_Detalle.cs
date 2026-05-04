using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigPrestamo_Detalle : IEntityTypeConfiguration<Prestamo_detalle>
    {
        public void Configure(EntityTypeBuilder<Prestamo_detalle> builder)
        {
            builder.ToTable("Prestamo_detalle");

            builder.HasKey(e => e.Id);

            builder.Property(x => x.Codigo).IsRequired();

            builder.Property(x => x.Nombre).IsRequired();

            builder.Property(x => x.Cantidad).IsRequired();

            builder.Property(x => x.Precio).HasPrecision(10, 2);

            builder.HasOne(x => x.Prestamo)
                .WithMany(x => x.Detalle)
                .HasForeignKey(x => x.Id_Prestamo)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fx_pretamos_pretamodetalle");
        }
    }
}
