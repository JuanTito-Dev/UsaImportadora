using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.Exceptions.GenericExceptions;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class ProductoRepositorio : GenericRepositorio<Producto>, IProductoRepositorio
    {
        private readonly DbSet<Producto> datos;
        private readonly AppDbContext _db;
        public ProductoRepositorio(AppDbContext _db) :base(_db)
        {
            datos = _db.Set<Producto>();
            this._db = _db;
        }

        public override IQueryable<Producto> Query()
        {
            return datos.AsNoTracking().Where(x => x.Activo == true);
        }

        public override async Task<bool> Eliminar(int Id)
        {
            var rows = await datos.Where(x => x.Id == Id && x.Activo == true)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Activo, false)
                    .SetProperty(x => x.Stock_Actual, 0)
                    .SetProperty(x => x.StockReservado, 0)
                    .SetProperty(x => x.FechaEliminacion, DateTime.UtcNow));

            if (rows == 0) throw new EntidadNoEncontradaException(typeof(Producto).Name);

            await _db.Set<PiezaKit>()
                .Where(p => p.Id_Producto == Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockActual, 0)
                    .SetProperty(p => p.StockReservado, 0));

            return true;
        }

        public async Task<Producto?> GetProductoforCodigo(string codigo)
        {
            return await datos.Include(x => x.PiezasKit).FirstOrDefaultAsync(x => x.Codigo == codigo);
        }

        public async Task<Producto?> ObtenerConPiezas(int id)
        {
            return await datos.Include(x => x.PiezasKit).FirstOrDefaultAsync(x => x.Id == id);
        }

        public IQueryable<Producto> GetProductos()
        {
            return datos.AsQueryable();
        }

        public async Task<List<Producto>> GetProductosConHistorial()
        {
            return await datos
                .Where(x => x.Activo)
                .Include(x => x.HistorialPrecios)
                .ToListAsync();
        }
    }
}
