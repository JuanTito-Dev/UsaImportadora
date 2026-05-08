using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.MargenGananciaDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UsuarioRoles.Admin)]
    public class MargenGananciaController(
        IMargenGananciaRepositorio _margen,
        ITipoCambioRepositorio _tipoCambio,
        IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Actualizar(DtoMargenGanancia datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var tipoCambio = await _tipoCambio.GetUnico();
            if (tipoCambio is null) return BadRequest(new { message = "No hay tipo de cambio registrado. Configúralo antes de actualizar el margen." });

            var margen = await _margen.GetUnico();

            if (margen is null)
            {
                margen = new MargenGanancia(datos.Valor);
                await _margen.Crear(margen);
                await _margen.GuardarAsync();
            }
            else
            {
                margen.Actualizar(datos.Valor);
            }

            var productos = _db.productos.GetProductos().ToList();

            foreach (var producto in productos)
            {
                var nuevoPrecio = producto.Costo * tipoCambio.PrecioDolar * (1 + datos.Valor / 100);
                var historial = producto.CambiarPrecio(producto.Costo, nuevoPrecio, tipoCambio.PrecioDolar, "Actualización por margen de ganancia");
                await _db.historialPrecios.Crear(historial);
            }

            await _db.SaveUnitWork();

            return Ok(new { message = "Margen actualizado y precios recalculados.", productosActualizados = productos.Count });
        }
    }
}
