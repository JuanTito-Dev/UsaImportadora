using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using UsaAutoPartes.Api.Hubs;
using UsaAutoPartes.Application.Dtos.VentaDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.CajaEnums;
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
        IMovimientoCajaRepositorio _movimientos,
        IHubContext<VentasHub> _hub) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = $"{UsuarioRoles.Cajero},{UsuarioRoles.Admin}")]
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
        [Authorize(Roles = $"{UsuarioRoles.Cajero},{UsuarioRoles.Admin}")]
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
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
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
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
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
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
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

        [HttpPost("{id:int}/Items/{itemId:int}/Confirmar")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> ConfirmarItem(int id, int itemId)
        {
            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista) return BadRequest(new { message = "La orden no está lista para escaneo." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (item.EsParcial) return BadRequest(new { message = "Use el endpoint de piezas para ítems parciales." });
            if (item.Estado == EstadosOrdenItem.Confirmado) return BadRequest(new { message = "El ítem ya fue confirmado." });
            if (item.Estado == EstadosOrdenItem.Incompleto) return BadRequest(new { message = "El ítem está marcado como incompleto." });

            var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });

            if (!producto.EsKit)
            {
                producto.LiberarReserva(item.Cantidad);
                producto.Descontar(item.Cantidad);
            }
            else
            {
                foreach (var pieza in producto.PiezasKit)
                {
                    var cantPieza = item.Cantidad * pieza.CantidadPorKit;
                    pieza.LiberarReserva(cantPieza);
                    pieza.DescontarStock(cantPieza);
                }
                producto.Stock_Actual = producto.CalcularStockKit();
            }

            item.Confirmar(item.PrecioUnitario);
            await _db.SaveUnitWork();

            return Ok(new { message = "Ítem confirmado." });
        }

        [HttpPost("{id:int}/Items/{itemId:int}/Piezas/{piezaItemId:int}/Confirmar")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> ConfirmarPieza(int id, int itemId, int piezaItemId, DtoConfirmarPieza datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista) return BadRequest(new { message = "La orden no está lista para escaneo." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (!item.EsParcial) return BadRequest(new { message = "Este ítem no es parcial." });
            if (item.Estado == EstadosOrdenItem.Incompleto) return BadRequest(new { message = "El ítem está marcado como incompleto." });

            var piezaItem = item.Piezas.FirstOrDefault(x => x.Id == piezaItemId);
            if (piezaItem is null) return NotFound(new { message = "Pieza no encontrada." });
            if (piezaItem.Confirmado) return BadRequest(new { message = "La pieza ya fue confirmada." });

            var pieza = await _db.piezasKit.Obtener(piezaItem.Id_Pieza);
            pieza.LiberarReserva(piezaItem.Cantidad);
            pieza.DescontarStock(piezaItem.Cantidad);

            var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
            if (producto is not null)
                producto.Stock_Actual = producto.CalcularStockKit();

            piezaItem.Confirmar(datos.PrecioUnitario);

            if (item.Estado == EstadosOrdenItem.Pendiente)
                item.Estado = EstadosOrdenItem.Confirmado;

            await _db.SaveUnitWork();

            return Ok(new { message = "Pieza confirmada." });
        }

        [HttpPost("{id:int}/Completar")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> Completar(int id, DtoCompletarOrden datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista) return BadRequest(new { message = "La orden no está lista para completar." });

            // Liberar reservas de ítems no confirmados
            foreach (var item in orden.Items.Where(i => i.Estado != EstadosOrdenItem.Confirmado))
            {
                if (!item.EsParcial)
                {
                    var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
                    if (producto is null) continue;

                    if (!producto.EsKit)
                        producto.LiberarReserva(item.Cantidad);
                    else
                        foreach (var pieza in producto.PiezasKit)
                            pieza.LiberarReserva(item.Cantidad * pieza.CantidadPorKit);
                }
                else
                {
                    foreach (var piezaItem in item.Piezas.Where(p => !p.Confirmado))
                    {
                        var pieza = await _db.piezasKit.Obtener(piezaItem.Id_Pieza);
                        pieza.LiberarReserva(piezaItem.Cantidad);
                    }
                }
            }

            // Calcular total de ítems confirmados
            decimal total = 0;
            foreach (var item in orden.Items)
            {
                if (item.Estado == EstadosOrdenItem.Incompleto) continue;

                if (!item.EsParcial)
                {
                    if (item.Estado == EstadosOrdenItem.Confirmado)
                        total += (item.PrecioUnitario * item.Cantidad) - item.MontoDescuento;
                }
                else
                {
                    total += item.Piezas
                        .Where(p => p.Confirmado)
                        .Sum(p => p.PrecioUnitario * p.Cantidad);
                }
            }

            var movimiento = new MovimientoCaja(
                orden.Id_Caja,
                TipoMovimiento.Ingreso,
                CategoriaMovimiento.Ventas,
                datos.TipoPago,
                total,
                $"Venta #{orden.Id}"
            );

            await _movimientos.Crear(movimiento);

            orden.Completar();
            await _db.SaveUnitWork();

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenCompletada", new
            {
                orden.Id,
                total
            });

            return Ok(new { message = "Orden completada.", total });
        }
    }
}
