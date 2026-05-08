using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.IRepositorio;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class UnitWork : IUnitWork
    {
        public AppDbContext db;
        public IProductoRepositorio productos { get; private set;  }
        public IProveedorRepositorio proveedores { get; private set; }
        public IImportacionRepositorio importaciones { get; private set; }

        public IHistorialPrecioRepositorio historialPrecios { get; private set; }

        public IPrestamoRepositorio prestamos {  get; private set; }

        public IPiezaKitRepositorio piezasKit { get; private set; }

        public IOrdenVentaRepositorio ordenesVenta { get; private set; }

        public UnitWork(IProductoRepositorio _productos,
            AppDbContext _db, IProveedorRepositorio proveedor,
            IImportacionRepositorio _impoop,
            IHistorialPrecioRepositorio _historialPrecios,
            IPrestamoRepositorio prestamos,
            IPiezaKitRepositorio _piezasKit,
            IOrdenVentaRepositorio _ordenesVenta
            )
        {
            productos = _productos;
            db = _db;
            this.proveedores = proveedor;
            importaciones = _impoop;
            historialPrecios = _historialPrecios;
            this.prestamos = prestamos;
            piezasKit = _piezasKit;
            ordenesVenta = _ordenesVenta;
        }
        public void Dispose()
        {
            db.Dispose();
        }

        public async Task<int> SaveUnitWork()
        {
            return await db.SaveChangesAsync();
        }
    }
}
