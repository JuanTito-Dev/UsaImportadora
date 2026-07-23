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
            return datos.AsNoTracking().Include(x => x.PiezasKit).Where(x => x.Activo == true);
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

        public async Task<Producto?> GetProductoforCodigo(string codigo, int? marcaId = null)
        {
            var q = datos.Include(x => x.PiezasKit).Where(x => x.Codigo == codigo);
            q = q.Where(x => x.MarcaId == marcaId);
            return await q.FirstOrDefaultAsync();
        }

        public async Task<Producto?> BuscarPorCodigoEscaneo(string codigo)
        {
            codigo = codigo.Trim();
            if (string.IsNullOrEmpty(codigo))
                return null;

            var baseQ = datos
                .Include(x => x.Marca)
                .Include(x => x.PiezasKit)
                .Where(x => x.Activo);

            var exacto = await baseQ.FirstOrDefaultAsync(x =>
                x.Codigo == codigo || x.CodigoAux == codigo || x.CodigoAux2 == codigo);
            if (exacto is not null)
                return exacto;

            var guion = codigo.IndexOf('-');
            if (guion > 0)
            {
                var prefijo = codigo[..guion];
                var resto = codigo[(guion + 1)..];
                if (!string.IsNullOrEmpty(prefijo) && !string.IsNullOrEmpty(resto))
                {
                    var porPrefijo = await baseQ.FirstOrDefaultAsync(x =>
                        x.Marca != null
                        && x.Marca.Prefijo == prefijo
                        && (x.Codigo == resto || x.Codigo == codigo));
                    if (porPrefijo is not null)
                        return porPrefijo;
                }
            }

            return null;
        }

        public IQueryable<Producto> BuscarPorTermino(string termino)
        {
            termino = termino.Trim();
            if (string.IsNullOrEmpty(termino))
                return datos.Where(x => false);

            var pattern = $"%{termino}%";
            return datos
                .AsNoTracking()
                .Include(x => x.Marca)
                .Include(x => x.PiezasKit)
                .Where(x => x.Activo)
                .Where(x =>
                    EF.Functions.ILike(x.Codigo, pattern)
                    || EF.Functions.ILike(x.Nombre, pattern)
                    || EF.Functions.ILike(x.CodigoAux, pattern)
                    || EF.Functions.ILike(x.CodigoAux2, pattern)
                    || (x.Marca != null && EF.Functions.ILike(x.Marca.Nombre, pattern))
                    || x.PiezasKit.Any(p => EF.Functions.ILike(p.Nombre, pattern))
                    || x.PiezasKit.Any(p => EF.Functions.ILike(p.CodigoPieza, pattern))
                    || (x.Descripcion != null && EF.Functions.ILike(x.Descripcion, pattern))
                    || (x.Marca != null && x.Marca.Prefijo != null && EF.Functions.ILike(x.Marca.Prefijo + "-" + x.Codigo, pattern)));
        }

        public async Task<Producto?> ObtenerConPiezas(int id)
        {
            return await datos.Include(x => x.PiezasKit).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Producto?> ObtenerConPiezasYMarca(int id)
        {
            return await datos
                .Include(x => x.PiezasKit)
                .Include(x => x.Marca)
                .FirstOrDefaultAsync(x => x.Id == id);
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
