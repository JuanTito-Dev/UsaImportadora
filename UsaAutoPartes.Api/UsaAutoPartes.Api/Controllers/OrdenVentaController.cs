using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using UsaAutoPartes.Api.Hubs;
using UsaAutoPartes.Application.Dtos.VentaDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Enum.CajaEnums;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;
using UsaAutoPartes.Domain.Enum.VentaEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdenVentaController(
        IOrdenVentaRepositorio _ordenes,
        ICajaRepositorio _cajas,
        IHubContext<VentasHub> _hub) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = UsuarioRoles.Cajero)]
        public async Task<IActionResult> Crear(DtoCrearOrden datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var caja = await _cajas.GetCajaActivaByUsuario(userId);
            if (caja is null) return BadRequest(new { message = "No tienes una caja abierta." });

            var orden = datos.Crear(userId, caja.Id);

            await _ordenes.Crear(orden);
            await _ordenes.GuardarAsync();

            await _hub.Clients.Group("Almaceneros").SendAsync("NuevaOrden", new
            {
                orden.Id,
                orden.Fecha,
                CantItems = orden.Items.Count,
                orden.Id_Cliente
            });

            return Created("", new { message = "Orden creada.", ordenId = orden.Id });
        }

        [HttpPost("{id:int}/Cancelar")]
        [Authorize(Roles = UsuarioRoles.Cajero)]
        public async Task<IActionResult> Cancelar(int id, [FromBody] string? nota)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _ordenes.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Cajero != userId) return Forbid();
            if (orden.Estado == EstadosOrden.Completada) return BadRequest(new { message = "No se puede cancelar una orden completada." });
            if (orden.Estado == EstadosOrden.Cancelada) return BadRequest(new { message = "La orden ya está cancelada." });

            orden.Cancelar(nota);
            await _ordenes.GuardarAsync();

            if (orden.Id_Almacenero.HasValue)
            {
                await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenCancelada", new
                {
                    orden.Id,
                    nota
                });
            }

            return Ok(new { message = "Orden cancelada." });
        }

        [HttpPost("{id:int}/Aceptar")]
        [Authorize(Roles = UsuarioRoles.Almacenero)]
        public async Task<IActionResult> Aceptar(int id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _ordenes.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Pendiente) return BadRequest(new { message = "La orden ya fue aceptada o no está disponible." });

            orden.Aceptar(userId);
            await _ordenes.GuardarAsync();

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenAceptada", new
            {
                orden.Id,
                AlmaceneroId = userId
            });

            return Ok(new { message = "Orden aceptada." });
        }

        [HttpPost("{id:int}/Items/{itemId:int}/Incompleto")]
        [Authorize(Roles = UsuarioRoles.Almacenero)]
        public async Task<IActionResult> MarcarItemIncompleto(int id, int itemId, DtoMarcarIncompleto datos)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _ordenes.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });

            item.MarcarIncompleto(datos.Nota);
            await _ordenes.GuardarAsync();

            return Ok(new { message = "Ítem marcado como incompleto." });
        }

        [HttpPost("{id:int}/Lista")]
        [Authorize(Roles = UsuarioRoles.Almacenero)]
        public async Task<IActionResult> MarcarLista(int id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _ordenes.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            orden.MarcarLista();
            await _ordenes.GuardarAsync();

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenLista", new
            {
                orden.Id
            });

            return Ok(new { message = "Orden marcada como lista." });
        }
    }
}
