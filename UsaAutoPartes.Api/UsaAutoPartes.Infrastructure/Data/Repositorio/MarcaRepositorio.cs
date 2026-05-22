using Microsoft.EntityFrameworkCore;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class MarcaRepositorio : GenericRepositorio<Marca>, IMarcaRepositorio
    {
        private readonly DbSet<Marca> _Marcas;

        public MarcaRepositorio(AppDbContext db) : base(db)
        {
            _Marcas = db.Set<Marca>();
        }

        public async Task<Marca?> ObtenerPorNombre(string nombre)
        {
            return await _Marcas.FirstOrDefaultAsync(m => m.Nombre == nombre);
        }

        public IQueryable<Marca> MarcaQuery()
        {
            return _Marcas.AsQueryable();
        }
    }
}
