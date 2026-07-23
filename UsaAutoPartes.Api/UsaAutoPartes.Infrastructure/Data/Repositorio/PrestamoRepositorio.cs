using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class PrestamoRepositorio : GenericRepositorio<Prestamo>, IPrestamoRepositorio
    {
        private readonly DbSet<Prestamo> _Set;
        public PrestamoRepositorio(AppDbContext _db) : base(_db)
        {
            _Set = _db.Set<Prestamo>();
        }

        public async Task<Prestamo?> ObtenerConDetalle(int id)
        {
            return await _Set
                .Include(x => x.Detalle)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
