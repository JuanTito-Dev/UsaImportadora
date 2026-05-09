using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.AjusteStockDtos;
using UsaAutoPartes.Application.Exceptions.GenericExceptions;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AjusteStockController(IUnitWork _db) : ControllerBase
    {
        [HttpPost("{productoId:int}")]
        public async Task<IActionResult> AjustarStock(int productoId, DtoAjusteStock datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.productos.ObtenerConPiezas(productoId);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });

            if (producto.EsKit && !producto.PiezasKit.Any())
                return BadRequest(new { message = "Este kit no tiene piezas configuradas. Agrega piezas antes de ajustar el stock." });

            int cantidadAnterior = producto.EsKit
                ? producto.CalcularStockKit()
                : producto.Stock_Actual;

            int delta = datos.NuevaCantidad - cantidadAnterior;

            if (delta == 0)
                return BadRequest(new { message = "El stock ya es igual a la cantidad indicada." });

            if (producto.EsKit)
            {
                if (delta < 0)
                {
                    var error = producto.ValidarPiezasSuficientes(datos.NuevaCantidad);
                    if (error != null) throw new StockInsuficienteException(error);

                    producto.DescontarKit(Math.Abs(delta));
                }
                else
                {
                    producto.AgregarStockKit(delta);
                }
            }
            else
            {
                producto.Stock_Actual = datos.NuevaCantidad;
            }

            var ajuste = new AjusteStock
            {
                Id_Producto = productoId,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = producto.Stock_Actual,
                Motivo = datos.Motivo,
                Nota = datos.Nota
            };

            await _db.ajustesStock.Crear(ajuste);
            await _db.SaveUnitWork();

            return Ok(new
            {
                message = "Stock ajustado.",
                cantidadAnterior,
                cantidadNueva = producto.Stock_Actual,
                delta
            });
        }

        [HttpPost("{productoId:int}/Piezas/{piezaId:int}")]
        public async Task<IActionResult> AjustarStockPieza(int productoId, int piezaId, DtoAjusteStock datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.productos.ObtenerConPiezas(productoId);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });
            if (!producto.EsKit) return BadRequest(new { message = "El producto no es un kit." });

            var pieza = producto.PiezasKit.FirstOrDefault(p => p.Id == piezaId);
            if (pieza is null) return NotFound(new { message = "Pieza no encontrada en este kit." });

            int cantidadAnterior = pieza.StockActual;
            int delta = datos.NuevaCantidad - cantidadAnterior;

            if (delta == 0)
                return BadRequest(new { message = "El stock de la pieza ya es igual a la cantidad indicada." });

            pieza.StockActual = datos.NuevaCantidad;
            producto.Stock_Actual = producto.CalcularStockKit();

            var ajuste = new AjusteStock
            {
                Id_Producto = productoId,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = datos.NuevaCantidad,
                Motivo = $"[Pieza: {pieza.Nombre}] {datos.Motivo}",
                Nota = datos.Nota
            };

            await _db.ajustesStock.Crear(ajuste);
            await _db.SaveUnitWork();

            return Ok(new
            {
                message = "Stock de pieza ajustado.",
                cantidadAnterior,
                cantidadNueva = datos.NuevaCantidad,
                stockKitRecalculado = producto.Stock_Actual,
                delta
            });
        }
    }
}
