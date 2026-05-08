using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using UsaAutoPartes.Api.Hubs;
using UsaAutoPartes.Application.Dtos.VentaDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;
using UsaAutoPartes.Domain.Enum.VentaEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdenVentaController(
        IUnitWork _db,
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

            foreach (var itemDto in datos.Items)
            {
                if (!itemDto.EsParcial)
                {
                    var producto = await _db.productos.ObtenerConPiezas(itemDto.Id_Producto);
                    if (producto is null) return NotFound(new { message = $"Producto {itemDto.Id_Producto} no encontrado." });

                    if (!producto.EsKit)
                    {
                        var disponible = producto.Stock_Actual - producto.StockReservado;
                        if (disponible < itemDto.Cantidad)
                            return BadRequest(new { message = $"Stock insuficiente para {producto.Nombre}. Disponible: {disponible}." });
                        producto.Reservar(itemDto.Cantidad);
                    }
                    else
                    {
                        foreach (var pieza in producto.PiezasKit)
                        {
                            var cantidadPieza = itemDto.Cantidad * pieza.CantidadPorKit;
                            var disponiblePieza = pieza.StockActual - pieza.StockReservado;
                            if (disponiblePieza < cantidadPieza)
                                return BadRequest(new { message = $"Stock insuficiente de {pieza.Nombre} para el kit {producto.Nombre}. Disponible: {disponiblePieza}." });
                            pieza.Reservar(cantidadPieza);
                        }
                    }
                }
                else
                {
                    if (itemDto.Piezas is null || !itemDto.Piezas.Any())
                        return BadRequest(new { message = "Orden parcial requiere especificar piezas." });

                    foreach (var piezaDto in itemDto.Piezas)
                    {
                        var pieza = await _db.piezasKit.Obtener(piezaDto.Id_Pieza);
                        var disponible = pieza.StockActual - pieza.StockReservado;
                        if (disponible < piezaDto.Cantidad)
                            return BadRequest(new { message = $"Stock insuficiente de pieza {pieza.Nombre}. Disponible: {disponible}." });
                        pieza.Reservar(piezaDto.Cantidad);
                    }
                }
            }

            var orden = datos.Crear(userId, caja.Id);
            await _db.ordenesVenta.Crear(orden);
            await _db.SaveUnitWork();

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

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Cajero != userId) return Forbid();
            if (orden.Estado == EstadosOrden.Completada) return BadRequest(new { message = "No se puede cancelar una orden completada." });
            if (orden.Estado == EstadosOrden.Cancelada) return BadRequest(new { message = "La orden ya está cancelada." });

            foreach (var item in orden.Items)
            {
                if (!item.EsParcial)
                {
                    var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
                    if (producto is null) continue;

                    if (!producto.EsKit)
                    {
                        producto.LiberarReserva(item.Cantidad);
                    }
                    else
                    {
                        foreach (var pieza in producto.PiezasKit)
                            pieza.LiberarReserva(item.Cantidad * pieza.CantidadPorKit);
                    }
                }
                else
                {
                    foreach (var piezaItem in item.Piezas)
                    {
                        var pieza = await _db.piezasKit.Obtener(piezaItem.Id_Pieza);
                        pieza.LiberarReserva(piezaItem.Cantidad);
                    }
                }
            }

            orden.Cancelar(nota);
            await _db.SaveUnitWork();

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

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Pendiente) return BadRequest(new { message = "La orden ya fue aceptada o no está disponible." });

            orden.Aceptar(userId);
            await _db.SaveUnitWork();

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

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });

            item.MarcarIncompleto(datos.Nota);
            await _db.SaveUnitWork();

            return Ok(new { message = "Ítem marcado como incompleto." });
        }

        [HttpPost("{id:int}/Lista")]
        [Authorize(Roles = UsuarioRoles.Almacenero)]
        public async Task<IActionResult> MarcarLista(int id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            orden.MarcarLista();
            await _db.SaveUnitWork();

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenLista", new
            {
                orden.Id
            });

            return Ok(new { message = "Orden marcada como lista." });
        }
    }
}
