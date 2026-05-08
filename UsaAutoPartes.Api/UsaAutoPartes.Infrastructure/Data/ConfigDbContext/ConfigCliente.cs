using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.ConfigDbContext
{
    public class ConfigCliente : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Cliente");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Apellido).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Telefono).IsRequired().HasMaxLength(20);

            builder.HasIndex(x => x.Telefono).HasDatabaseName("IX_Cliente_Telefono");
        }
    }
}
