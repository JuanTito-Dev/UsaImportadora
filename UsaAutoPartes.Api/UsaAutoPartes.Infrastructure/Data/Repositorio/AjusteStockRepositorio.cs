using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class AjusteStockRepositorio : GenericRepositorio<AjusteStock>, IAjusteStockRepositorio
    {
        public AjusteStockRepositorio(AppDbContext context) : base(context)
        {
        }
    }
}
