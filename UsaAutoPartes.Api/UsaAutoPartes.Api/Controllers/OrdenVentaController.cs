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

            var nota = datos.CantidadEncontrada.HasValue && datos.CantidadEncontrada.Value > 0
                ? $"Encontró {datos.CantidadEncontrada.Value} de {item.Cantidad}" + (string.IsNullOrWhiteSpace(datos.Nota) ? "" : $" — {datos.Nota}")
                : datos.Nota;
            item.MarcarIncompleto(nota);
            await _db.SaveUnitWork();

            return Ok(new { message = "Ítem marcado como incompleto." });
        }

        [HttpDelete("{id:int}/Items/{itemId:int}/Incompleto")]
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> RevertirItemIncompleto(int id, int itemId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (item.Estado != EstadosOrdenItem.Incompleto) return BadRequest(new { message = "El ítem no está marcado como incompleto." });

            item.RevertirIncompleto();
            await _db.SaveUnitWork();

            return Ok(new { message = "Faltante revertido." });
        }

        [HttpPost("{id:int}/Items/{itemId:int}/Piezas/{piezaItemId:int}/Incompleto")]
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> MarcarPiezaIncompleta(int id, int itemId, int piezaItemId, DtoMarcarIncompleto datos)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (!item.EsParcial) return BadRequest(new { message = "Solo ítems parciales tienen piezas." });

            var piezaItem = item.Piezas.FirstOrDefault(x => x.Id == piezaItemId);
            if (piezaItem is null) return NotFound(new { message = "Pieza no encontrada." });

            var pieza = await _db.piezasKit.Obtener(piezaItem.Id_Pieza);
            if (pieza is not null) pieza.LiberarReserva(piezaItem.Cantidad);

            piezaItem.MarcarIncompleto(datos.Nota);

            if (item.Piezas.All(p => p.NotaIncompleto != null))
                item.MarcarIncompleto(datos.Nota);

            await _db.SaveUnitWork();

            return Ok(new { message = "Pieza marcada como incompleta." });
        }

        [HttpDelete("{id:int}/Items/{itemId:int}/Piezas/{piezaItemId:int}/Incompleto")]
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> RevertirPiezaIncompleta(int id, int itemId, int piezaItemId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Id_Almacenero != userId) return Forbid();
            if (orden.Estado != EstadosOrden.Aceptada) return BadRequest(new { message = "La orden no está en estado Aceptada." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });

            var piezaItem = item.Piezas.FirstOrDefault(x => x.Id == piezaItemId);
            if (piezaItem is null) return NotFound(new { message = "Pieza no encontrada." });
            if (piezaItem.NotaIncompleto is null) return BadRequest(new { message = "La pieza no está marcada como incompleta." });

            var pieza = await _db.piezasKit.Obtener(piezaItem.Id_Pieza);
            if (pieza is not null) pieza.Reservar(piezaItem.Cantidad);

            piezaItem.RevertirIncompleto();

            if (item.Estado == EstadosOrdenItem.Incompleto)
                item.RevertirIncompleto();

            await _db.SaveUnitWork();

            return Ok(new { message = "Faltante de pieza revertido." });
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
            if (orden.Estado != EstadosOrden.Lista && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está lista para escaneo." });

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
            if (orden.Estado != EstadosOrden.Lista && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está lista para escaneo." });

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

        [HttpPost("{id:int}/AgregarItem")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> AgregarItem(int id, DtoAgregarItemOrden datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está lista para escaneo." });

            var producto = await _db.productos.ObtenerConPiezas(datos.Id_Producto);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });
            if (producto.EsKit) return BadRequest(new { message = "Los kits no pueden agregarse en escaneo." });

            var disponible = producto.Stock_Actual - producto.StockReservado;
            if (disponible < datos.Cantidad)
                return BadRequest(new { message = $"Stock insuficiente. Disponible: {disponible}." });

            producto.Reservar(datos.Cantidad);

            var item = new OrdenVentaItem(datos.Id_Producto, datos.Cantidad, false, producto.Precio, null, 0)
            {
                Id_Orden = id
            };
            orden.Items.Add(item);
            orden.MarcarConFaltantes();
            await _db.SaveUnitWork();

            await _hub.Clients.Group("Almaceneros").SendAsync("NuevoItemAgregado", new
            {
                ordenId = id,
                itemId = item.Id,
                productoNombre = producto.Nombre,
                codigo = producto.Codigo,
                cantidad = item.Cantidad
            });

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenConFaltantes", new
            {
                ordenId = id
            });

            return Ok(new
            {
                id = item.Id,
                id_Producto = item.Id_Producto,
                cantidad = item.Cantidad,
                precioUnitario = (double)item.PrecioUnitario,
                estado = item.Estado,
                producto = new { id = producto.Id, codigo = producto.Codigo, nombre = producto.Nombre, ubicacion = producto.Ubicacion }
            });
        }

        [HttpDelete("{id:int}/Items/{itemId:int}")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> EliminarItem(int id, int itemId)
        {
            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está lista para escaneo." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (item.Estado == EstadosOrdenItem.Confirmado) return BadRequest(new { message = "No se puede eliminar un ítem ya confirmado." });

            var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
            if (producto is not null)
                producto.LiberarReserva(item.Cantidad);

            orden.Items.Remove(item);
            await _db.SaveUnitWork();

            await _hub.Clients.Group("Almaceneros").SendAsync("ItemEliminado", new
            {
                ordenId = id,
                itemId
            });

            return Ok(new { message = "Ítem eliminado." });
        }

        [HttpPut("{id:int}/Items/{itemId:int}/Cantidad")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> ActualizarCantidadItem(int id, int itemId, DtoActualizarCantidadItem datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está lista para escaneo." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (item.Estado == EstadosOrdenItem.Confirmado) return BadRequest(new { message = "No se puede modificar un ítem ya confirmado." });

            var producto = await _db.productos.ObtenerConPiezas(item.Id_Producto);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });

            var diferencia = datos.Cantidad - item.Cantidad;
            if (diferencia > 0)
            {
                var disponible = producto.Stock_Actual - producto.StockReservado;
                if (disponible < diferencia)
                    return BadRequest(new { message = $"Stock insuficiente. Disponible adicional: {disponible}." });
                producto.Reservar(diferencia);
            }
            else if (diferencia < 0)
            {
                producto.LiberarReserva(-diferencia);
            }

            item.Cantidad = datos.Cantidad;
            await _db.SaveUnitWork();

            await _hub.Clients.Group("Almaceneros").SendAsync("CantidadItemActualizada", new
            {
                ordenId = id,
                itemId,
                nuevaCantidad = datos.Cantidad
            });

            return Ok(new { message = "Cantidad actualizada.", nuevaCantidad = datos.Cantidad });
        }

        [HttpPost("{id:int}/Items/{itemId:int}/MarcarListoIndividual")]
        [Authorize(Roles = $"{UsuarioRoles.Almacenero},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> MarcarItemListoIndividual(int id, int itemId)
        {
            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Aceptada && orden.Estado != EstadosOrden.ConFaltantes)
                return BadRequest(new { message = "La orden no está en preparación." });

            var item = orden.Items.FirstOrDefault(x => x.Id == itemId);
            if (item is null) return NotFound(new { message = "Ítem no encontrado." });
            if (item.Estado != EstadosOrdenItem.Pendiente) return BadRequest(new { message = "El ítem no está en estado pendiente." });

            item.MarcarListoIndividual();

            // Si estaba en ConFaltantes y todos los items ya están resueltos, volver a Lista
            if (orden.Estado == EstadosOrden.ConFaltantes)
            {
                var todosResueltos = orden.Items.All(i =>
                    i.Estado == EstadosOrdenItem.Confirmado ||
                    i.Estado == EstadosOrdenItem.Incompleto ||
                    i.Estado == EstadosOrdenItem.ListoIndividual);

                if (todosResueltos)
                {
                    orden.MarcarLista();
                    await _db.SaveUnitWork();

                    await _hub.Clients.Group($"orden-{id}").SendAsync("ItemListoParaScaneo", new { ordenId = id, itemId });
                    await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenLista", new { orden.Id });

                    return Ok(new { message = "Ítem marcado como listo. Orden vuelve a Lista." });
                }
            }

            await _db.SaveUnitWork();

            await _hub.Clients.Group($"orden-{id}").SendAsync("ItemListoParaScaneo", new
            {
                ordenId = id,
                itemId
            });

            return Ok(new { message = "Ítem marcado como listo." });
        }

        [HttpPost("{id:int}/MarcarEsperandoPago")]
        [Authorize(Roles = $"{UsuarioRoles.Operador},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> MarcarEsperandoPago(int id)
        {
            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.Lista) return BadRequest(new { message = "La orden no está lista para escaneo." });

            var itemsPendientes = orden.Items.Where(i =>
                i.Estado == EstadosOrdenItem.Pendiente ||
                i.Estado == EstadosOrdenItem.ListoIndividual).ToList();

            if (itemsPendientes.Count > 0)
                return BadRequest(new { message = "Hay ítems pendientes de escanear." });

            orden.MarcarEsperandoPago();
            await _db.SaveUnitWork();

            await _hub.Clients.Group($"orden-{id}").SendAsync("OrdenEsperandoPago", new
            {
                orden.Id
            });

            await _hub.Clients.Group("Cajeros").SendAsync("OrdenEsperandoPago", new
            {
                orden.Id
            });

            return Ok(new { message = "Orden marcada como esperando pago." });
        }

        [HttpPost("{id:int}/Completar")]
        [Authorize(Roles = $"{UsuarioRoles.Cajero},{UsuarioRoles.Admin}")]
        public async Task<IActionResult> Completar(int id, DtoCompletarOrden datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var orden = await _db.ordenesVenta.GetConItems(id);
            if (orden is null) return NotFound(new { message = "Orden no encontrada." });
            if (orden.Estado != EstadosOrden.EsperandoPago) return BadRequest(new { message = "La orden no está esperando pago." });

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

            var sumaPagos = datos.Pagos.Sum(p => p.Monto);
            if (Math.Abs(sumaPagos - total) > 0.01m)
                return BadRequest(new { message = $"La suma de pagos ({sumaPagos:F2}) no coincide con el total ({total:F2})." });

            foreach (var pago in datos.Pagos)
            {
                var movimiento = new MovimientoCaja(
                    orden.Id_Caja,
                    TipoMovimiento.Ingreso,
                    CategoriaMovimiento.Ventas,
                    pago.TipoPago,
                    pago.Monto,
                    $"Venta #{orden.Id}"
                );
                await _movimientos.Crear(movimiento);
            }

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
