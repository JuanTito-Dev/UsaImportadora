using Microsoft.EntityFrameworkCore;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class PiezaKitRepositorio(AppDbContext db) : GenericRepositorio<PiezaKit>(db), IPiezaKitRepositorio
    {
        private readonly DbSet<PiezaKit> _piezas = db.Set<PiezaKit>();

        public async Task<PiezaKit?> GetByCodigoUniversal(string codigoUniversal)
        {
            return await _piezas.FirstOrDefaultAsync(x => x.CodigoUniversal == codigoUniversal);
        }
    }
}
