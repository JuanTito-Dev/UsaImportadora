using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.PrestamoDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrestamoController(IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoPrestamoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var Prestamonew = datos.Crear();

            var Listadetalle = new List<Prestamo_detalle>();

            foreach(var item in datos.Detalles)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto is null) return NotFound(new { message = "Poducto no encontrado" });

                var detalle = item.Crear(producto.Nombre, producto.Precio);

                Listadetalle.Add(detalle);

                Prestamonew.SumarPrecio(detalle.Total());

                producto.Descontar(item.Cantidad);
            }

            Prestamonew.Detalle = Listadetalle;

            await _db.prestamos.Crear(Prestamonew);

            await _db.SaveUnitWork();
            
            return Created("", new { message = "Prestamo creado"});
        }

        [HttpPost("Cancelar/{Id:int}")]
        public async Task<IActionResult> Cancelar(int Id)
        {
            var prestamo = await _db.prestamos.Obtener(Id);

            prestamo.CancelarPedido();

            await _db.SaveUnitWork();

            return Ok(new { message = "Prestamo cancelado"});
        }
    }
}
