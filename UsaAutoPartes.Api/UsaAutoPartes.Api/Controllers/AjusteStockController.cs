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
                CantidadNueva = datos.NuevaCantidad,
                Motivo = datos.Motivo,
                Nota = datos.Nota
            };

            await _db.ajustesStock.Crear(ajuste);
            await _db.SaveUnitWork();

            return Ok(new
            {
                message = "Stock ajustado.",
                cantidadAnterior,
                cantidadNueva = datos.NuevaCantidad,
                delta
            });
        }
    }
}
