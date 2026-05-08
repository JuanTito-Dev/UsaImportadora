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
        public ProductoRepositorio(AppDbContext _db) :base(_db)
        {
            datos = _db.Set<Producto>();
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
    }
}
