using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class HistorialPrecioRepositorio : GenericRepositorio<HistorialPrecio>, IHistorialPrecioRepositorio
    {
        public HistorialPrecioRepositorio(AppDbContext _db) : base(_db)
        {
        }
    }
}
