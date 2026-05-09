using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UsaAutoPartes.Application.Dtos.UsuarioDtos;
using UsaAutoPartes.Application.Exceptions.GenericExceptions;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities.IdentityDb;

namespace UsaAutoPartes.Infrastructure.Data.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly UserManager<Usuario> _usuarios;
        private readonly AppDbContext _db;

        public UsuarioRepositorio(UserManager<Usuario> usuarios, AppDbContext db)
        {
            _usuarios = usuarios;
            _db = db;
        }

        public async Task<(Usuario? usuario, string? rol)> Me(string id)
        {
            var usuario = await _usuarios.FindByIdAsync(id);
            if (usuario is null) return (null, "");
            var rol = await _usuarios.GetRolesAsync(usuario);
            return (usuario, rol.FirstOrDefault());
        }

        public async Task<List<DtoUsuarioResponse>> GetTodosAsync()
        {
            var usuarios = await _usuarios.Users.ToListAsync();
            var horarios = await _db.HorariosBloqueo
                .Where(h => h.Activo)
                .ToDictionaryAsync(h => h.UsuarioId);

            var result = new List<DtoUsuarioResponse>();

            foreach (var u in usuarios)
            {
                var roles = await _usuarios.GetRolesAsync(u);
                var activo = !u.LockoutEnabled || !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow;
                var bloqueadoHasta = (!activo && u.LockoutEnd.HasValue && u.LockoutEnd.Value < DateTimeOffset.MaxValue)
                    ? u.LockoutEnd.Value.UtcDateTime
                    : (DateTime?)null;

                horarios.TryGetValue(u.Id, out var horario);

                result.Add(new DtoUsuarioResponse
                {
                    Id = u.Id.ToString(),
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email ?? string.Empty,
                    Rol = roles.FirstOrDefault() ?? string.Empty,
                    Activo = activo,
                    BloqueadoHasta = bloqueadoHasta,
                    Horario = horario is null ? null : new DtoHorarioResponse
                    {
                        HoraInicio = horario.HoraInicio.ToString("HH:mm"),
                        HoraFin = horario.HoraFin.ToString("HH:mm"),
                        Activo = horario.Activo,
                    }
                });
            }

            return result;
        }

        public async Task ToggleActivoAsync(string id)
        {
            var usuario = await _usuarios.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var activo = !usuario.LockoutEnabled || !usuario.LockoutEnd.HasValue || usuario.LockoutEnd.Value <= DateTimeOffset.UtcNow;

            if (activo)
            {
                await _usuarios.SetLockoutEnabledAsync(usuario, true);
                await _usuarios.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);
            }
            else
            {
                await _usuarios.SetLockoutEndDateAsync(usuario, null);
            }
        }

        public async Task DesactivarTodosAsync(string callerEmail)
        {
            var usuarios = await _usuarios.Users
                .Where(u => u.Email != callerEmail)
                .ToListAsync();

            foreach (var u in usuarios)
            {
                await _usuarios.SetLockoutEnabledAsync(u, true);
                await _usuarios.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
            }
        }

        public async Task ProgramarBloqueoAsync(DateTime hasta, string callerEmail)
        {
            var hastaUtc = new DateTimeOffset(hasta.ToUniversalTime(), TimeSpan.Zero);

            var usuarios = await _usuarios.Users
                .Where(u => u.Email != callerEmail)
                .ToListAsync();

            foreach (var u in usuarios)
            {
                await _usuarios.SetLockoutEnabledAsync(u, true);
                await _usuarios.SetLockoutEndDateAsync(u, hastaUtc);
            }
        }

        public async Task<DtoHorarioResponse?> GetHorarioAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid)) throw new EntidadNoEncontradaException("Usuario");
            var h = await _db.HorariosBloqueo.FirstOrDefaultAsync(x => x.UsuarioId == guid);
            if (h is null) return null;
            return new DtoHorarioResponse
            {
                HoraInicio = h.HoraInicio.ToString("HH:mm"),
                HoraFin = h.HoraFin.ToString("HH:mm"),
                Activo = h.Activo,
            };
        }

        public async Task SetHorarioAsync(string id, DtoSetHorario datos)
        {
            if (!Guid.TryParse(id, out var guid)) throw new EntidadNoEncontradaException("Usuario");
            _ = await _usuarios.FindByIdAsync(id) ?? throw new EntidadNoEncontradaException("Usuario");

            if (!TimeOnly.TryParseExact(datos.HoraInicio, "HH:mm", out var inicio))
                throw new ArgumentException("HoraInicio inválida. Formato: HH:mm");
            if (!TimeOnly.TryParseExact(datos.HoraFin, "HH:mm", out var fin))
                throw new ArgumentException("HoraFin inválida. Formato: HH:mm");

            var existing = await _db.HorariosBloqueo.FirstOrDefaultAsync(x => x.UsuarioId == guid);

            if (existing is null)
            {
                _db.HorariosBloqueo.Add(new Domain.Entities.IdentityDb.HorarioBloqueo
                {
                    UsuarioId = guid,
                    HoraInicio = inicio,
                    HoraFin = fin,
                    Activo = true,
                });
            }
            else
            {
                existing.HoraInicio = inicio;
                existing.HoraFin = fin;
                existing.Activo = true;
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteHorarioAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid)) throw new EntidadNoEncontradaException("Usuario");

            var h = await _db.HorariosBloqueo.FirstOrDefaultAsync(x => x.UsuarioId == guid);
            if (h is null) return;

            _db.HorariosBloqueo.Remove(h);

            // Si estaba bloqueado por horario, liberar
            var usuario = await _usuarios.FindByIdAsync(id);
            if (usuario is not null && usuario.BloqueoHorarioActivo)
            {
                await _usuarios.SetLockoutEndDateAsync(usuario, null);
                usuario.BloqueoHorarioActivo = false;
                await _usuarios.UpdateAsync(usuario);
            }

            await _db.SaveChangesAsync();
        }
    }
}
