using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsaAutoPartes.Application.Dtos.UsuarioDtos;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Application.IRepositorio
{
    public interface IUsuarioRepositorio
    {
        Task<(Usuario? usuario, string? rol)> Me(string id);
        Task<List<DtoUsuarioResponse>> GetTodosAsync();
        Task ToggleActivoAsync(string id);
        Task DesactivarTodosAsync(string callerEmail);
        Task ProgramarBloqueoAsync(DateTime hasta, string callerEmail);
        Task<DtoHorarioResponse?> GetHorarioAsync(string id);
        Task SetHorarioAsync(string id, DtoSetHorario datos);
        Task DeleteHorarioAsync(string id);
    }
}
