using Microsoft.EntityFrameworkCore;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class OrdenVentaRepositorio(AppDbContext db) : GenericRepositorio<OrdenVenta>(db), IOrdenVentaRepositorio
    {
        private readonly DbSet<OrdenVenta> _ordenes = db.Set<OrdenVenta>();

        public async Task<OrdenVenta?> GetConItems(int id)
        {
            return await _ordenes
                .Include(x => x.Items)
                    .ThenInclude(i => i.Piezas)
                .Include(x => x.Cliente)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public IQueryable<OrdenVenta> OrdenQuery()
        {
            return _ordenes.AsQueryable();
        }
    }
}
