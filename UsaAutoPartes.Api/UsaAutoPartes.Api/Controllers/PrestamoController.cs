using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.PrestamoDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{UsuarioRoles.Admin}")]
    public class PrestamoController(IUnitWork _db, IClienteRepositorio _clientes) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(DtoPrestamoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cliente = await _clientes.Obtener(datos.Id_Cliente);

            var Prestamonew = datos.Crear($"{cliente.Nombre} {cliente.Apellido}");

            var Listadetalle = new List<Prestamo_detalle>();

            foreach (var item in datos.Detalles)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto is null) return NotFound(new { message = "Producto no encontrado" });

                var detalle = item.Crear(producto.Nombre, producto.Precio, producto.Id);
                Listadetalle.Add(detalle);
                Prestamonew.SumarPrecio(detalle.Total());

                if (producto.EsKit)
                    producto.DescontarKit(item.Cantidad);
                else
                    producto.Descontar(item.Cantidad);
            }

            Prestamonew.Detalle = Listadetalle;

            await _db.prestamos.Crear(Prestamonew);

            await _db.SaveUnitWork();

            return Created("", new { message = "Prestamo creado" });
        }

        [HttpPost("Devolver/{Id:int}")]
        public async Task<IActionResult> Devolver(int Id)
        {
            var prestamo = await _db.prestamos.ObtenerConDetalle(Id);

            if (prestamo is null) return NotFound(new { message = "Prestamo no encontrado" });

            foreach (var detalle in prestamo.Detalle)
            {
                var producto = await _db.productos.ObtenerConPiezas(detalle.Id_Producto);
                if (producto is null) continue;

                if (producto.EsKit)
                    producto.AgregarStockKit(detalle.Cantidad);
                else
                    producto.Devolver(detalle.Cantidad);
            }

            prestamo.Devolver();

            await _db.SaveUnitWork();

            return Ok(new { message = "Prestamo devuelto" });
        }
    }
}
