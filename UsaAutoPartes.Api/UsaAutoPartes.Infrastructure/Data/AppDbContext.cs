using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Infrastructure.Data.ConfigDbContext;

namespace UsaAutoPartes.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<HistorialPrecio> HistorialPrecios { get; set; }

        public DbSet<Proveedor> Proveedores { get; set; }

        public DbSet<Importacion> Importaciones { get; set; }

        public DbSet<Importacion_Detalle> DetalleImportaciones { get; set; }

        public DbSet<Descuento> Descuentos { get; set; }

        public DbSet<Prestamo> Prestamos { get; set; }

        public DbSet<Prestamo_detalle> Prestamo_Detalles {  get; set; }

        public DbSet<PiezaKit> PiezasKit { get; set; }

        public DbSet<Caja> Cajas { get; set; }

        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }

        public DbSet<TipoCambio> TipoCambios { get; set; }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<MargenGanancia> MargenGanancias { get; set; }

        public DbSet<ConfigVenta> ConfigVentas { get; set; }

        public DbSet<OrdenVenta> OrdenesVenta { get; set; }

        public DbSet<OrdenVentaItem> OrdenesVentaItems { get; set; }

        public DbSet<OrdenVentaItemPieza> OrdenesVentaItemPiezas { get; set; }

        public DbSet<AjusteStock> AjustesStock { get; set; }

        public DbSet<HorarioBloqueo> HorariosBloqueo { get; set; }

        public DbSet<Marca> Marcas { get; set; }

        protected override void OnModelCreating(ModelBuilder Builder)
        {
           base.OnModelCreating(Builder);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigUsuario).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigRefreshToken).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigProducto).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigProveedor).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigImportacion).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigHistorialPrecio).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigImportacion_Detalle).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigDescuento).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigPrestamo).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigPrestamo_Detalle).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigTipoCambio).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigCliente).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigMargenGanancia).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigVentaConfig).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigOrdenVenta).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigOrdenVentaItem).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigOrdenVentaItemPieza).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigHorarioBloqueo).Assembly);
            Builder.ApplyConfigurationsFromAssembly(typeof(ConfigMarca).Assembly);
        }
    }
}
