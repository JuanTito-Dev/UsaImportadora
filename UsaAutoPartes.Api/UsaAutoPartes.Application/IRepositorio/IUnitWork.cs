using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IUnitWork : IDisposable
    {
        IProductoRepositorio productos {get; }

        IProveedorRepositorio proveedores {get; }

        IImportacionRepositorio importaciones { get; }

        IHistorialPrecioRepositorio historialPrecios { get; }

        IPrestamoRepositorio prestamos { get; }

        IPiezaKitRepositorio piezasKit { get; }

        IOrdenVentaRepositorio ordenesVenta { get; }

        Task<int> SaveUnitWork();
    }
}
