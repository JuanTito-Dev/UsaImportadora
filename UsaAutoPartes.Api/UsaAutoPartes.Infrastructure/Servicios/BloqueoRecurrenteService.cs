using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UsaAutoPartes.Domain.Entities.IdentityDb;
using UsaAutoPartes.Infrastructure.Data;

namespace UsaAutoPartes.Infrastructure.Servicios
{
    public class BloqueoRecurrenteService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BloqueoRecurrenteService> _logger;

        public BloqueoRecurrenteService(IServiceScopeFactory scopeFactory, ILogger<BloqueoRecurrenteService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await AplicarBloqueos(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Error en BloqueoRecurrenteService"); }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task AplicarBloqueos(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

            var ahora = TimeOnly.FromDateTime(DateTime.Now);

            var horarios = await db.HorariosBloqueo
                .Include(h => h.Usuario)
                .Where(h => h.Activo)
                .ToListAsync(ct);

            foreach (var h in horarios)
            {
                var enVentana = EstaEnVentana(ahora, h.HoraInicio, h.HoraFin);
                var usuario = h.Usuario;

                if (enVentana && !usuario.BloqueoHorarioActivo)
                {
                    var finBloqueo = CalcularFinBloqueo(h.HoraFin);
                    await userManager.SetLockoutEnabledAsync(usuario, true);
                    await userManager.SetLockoutEndDateAsync(usuario, finBloqueo);
                    usuario.BloqueoHorarioActivo = true;
                    await userManager.UpdateAsync(usuario);
                    _logger.LogInformation("Bloqueado por horario: {email}", usuario.Email);
                }
                else if (!enVentana && usuario.BloqueoHorarioActivo)
                {
                    await userManager.SetLockoutEndDateAsync(usuario, null);
                    usuario.BloqueoHorarioActivo = false;
                    await userManager.UpdateAsync(usuario);
                    _logger.LogInformation("Desbloqueado por horario: {email}", usuario.Email);
                }
            }
        }

        private static bool EstaEnVentana(TimeOnly ahora, TimeOnly inicio, TimeOnly fin)
        {
            if (inicio <= fin)
                return ahora >= inicio && ahora < fin;
            else // cruza medianoche (ej: 20:00 → 03:00)
                return ahora >= inicio || ahora < fin;
        }

        private static DateTimeOffset CalcularFinBloqueo(TimeOnly horaFin)
        {
            var ahora = DateTime.Now;
            var fin = ahora.Date.Add(horaFin.ToTimeSpan());
            if (fin <= ahora) fin = fin.AddDays(1);
            return new DateTimeOffset(fin, TimeZoneInfo.Local.GetUtcOffset(fin));
        }
    }
}
