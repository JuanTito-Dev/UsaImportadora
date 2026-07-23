using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IMarcaRepositorio : IGenericRepositorio<Marca>
    {
        Task<Marca?> ObtenerPorNombre(string nombre);
        Task<bool> PrefijoExiste(string prefijo);
        IQueryable<Marca> MarcaQuery();
    }
}
