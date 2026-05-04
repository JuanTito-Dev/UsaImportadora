using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.Dtos.ImportacionDtos;
using UsaAutoPartes.Application.Dtos.ProductosDtos;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities; 
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = UsuarioRoles.Admin)]
    public class ProductoController(IProductoRepositorio _producto, IUnitWork _db) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Crear(ProductoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = datos.AdaptarProducto();

            await _producto.Crear(producto);

            await _producto.GuardarAsync();

            return Created("", new { message = "Producto creado"});
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Editar(int Id, ProductoActualizar datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var productoBd = await _producto.Obtener(Id);

            productoBd = datos.Adapt(productoBd);

            await _producto.GuardarAsync();

            return Ok(new {message = "Producto editado"});
        }

        [HttpPost("CambiarPrecio/{Id:int}")]
        public async Task<IActionResult> Cambiarprecio(int Id, DtoProductoUPrecio datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var productoBd = await _db.productos.Obtener(Id);

            var precio = productoBd.CambiarPrecio(datos.Costo, datos.Precio, datos.ConversionABs, datos.Nota);

            await _db.historialPrecios.Crear(precio);

            await _db.SaveUnitWork();

            return Ok(new { message = "Precio cambiado" });

        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _producto.Eliminar(id);
            await _producto.GuardarAsync();
            return NoContent();
        }

        [HttpPost("lista")]
        public async Task<IActionResult> CrearLista(DtoListaProducto Lista)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (Lista.Productos == null || !Lista.Productos.Any())
                return BadRequest(new { mensaje = "La lista está vacía." });


            int CreadoCant = 0;
            int ActualizadoCant = 0;

            foreach (var item in Lista.Productos)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto != null)
                {
                    var precio = item.Actualizar(producto, "Actualizacion de la lista");
                    await _db.historialPrecios.Crear(precio);
                    ActualizadoCant++;
                }
                else
                {
                    var newproducto = item.Crear();
                    await _db.productos.Crear(newproducto);
                    CreadoCant++;

                }
            }

            await _db.SaveUnitWork();

            var res = new DtoRespuestaLista(ActualizadoCant, CreadoCant);

            return Ok(res);
        }

        [HttpPost("importacion")]
        public async Task<IActionResult> Importar(DtoImportacionLista datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var proveedor = await _db.proveedores.Obtener(datos.Id_Proveedor);

            if (datos.Productos == null || !datos.Productos.Any())
                return BadRequest(new { mensaje = "La lista está vacía." });

            int CreadoCant = 0;
            int ActualizadoCant = 0;

            var detallesImportacion = new List<Importacion_Detalle>();

            foreach (var item in datos.Productos)
            {
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo);

                if (producto != null)
                {
                    var precio = item.Actualizar(producto, "Actualizado por importacion");
                    await _db.historialPrecios.Crear(precio);

                    var detalle = item.CrearImportacionDetalle();
                    detalle.Tipo = "Stock+";
                    detallesImportacion.Add(detalle);
                    ActualizadoCant++;
                }
                else
                {
                    var newproducto = item.Crear();
                    await _db.productos.Crear(newproducto);
                    var detalle = item.CrearImportacionDetalle();
                    detalle.Tipo = "Nuevo";
                    detallesImportacion.Add(detalle);
                    CreadoCant++;
                }
            }

            proveedor.CanImportaciones++;
            proveedor.Total += datos.CostoTotal;

            var correlativo = await _db.importaciones.Count(x => x.Fecha.Year == datos.Fecha.Year) + 1;

            var importacion = new Importacion
            {
                Codigo = $"IMP-{datos.Fecha.Year}-{correlativo.ToString()}",
                Id_Proveedor = datos.Id_Proveedor,
                Fecha = datos.Fecha,
                Total = datos.CostoTotal,
                CantProductos = datos.Productos.Count(),
                F_Internacional = datos.F_Internacional,
                Aduana_Arancel = datos.Aduana_Arancel,
                Trasporte_Interno = datos.Trasporte_Interno,
                Detalles = detallesImportacion
            };

            await _db.importaciones.Crear(importacion);

            await _db.SaveUnitWork();

            var res = new DtoRespuestaLista(ActualizadoCant, CreadoCant);

            return Ok(res);
        }
    }
}
