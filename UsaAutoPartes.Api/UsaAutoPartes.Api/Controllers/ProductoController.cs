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
    public class ProductoController(
        IProductoRepositorio _producto,
        IUnitWork _db,
        IMargenGananciaRepositorio _margen,
        ILogger<ProductoController> _logger) : ControllerBase
    {
        private static decimal CalcularPrecioConMargen(decimal costoTotalBs, decimal margenValor)
            => Math.Ceiling(costoTotalBs * margenValor * 100) / 100;

        [HttpPost]
        public async Task<IActionResult> Crear(ProductoCrear datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = datos.AdaptarProducto();

            await _producto.Crear(producto);

            await _producto.GuardarAsync();

            return Created("", new { message = "Producto creado", id = producto.Id });
        }

        [HttpPut("{Id:int}")]
        public async Task<IActionResult> Editar(int Id, ProductoActualizar datos)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var productoBd = await _producto.Obtener(Id);

            productoBd.Codigo        = datos.Codigo;
            productoBd.CodigoAux     = datos.CodigoAux;
            productoBd.CodigoAux2    = datos.CodigoAux2;
            productoBd.Nombre        = datos.Nombre;
            productoBd.MarcaId       = datos.MarcaId;
            productoBd.Descripcion   = datos.Descripcion;
            productoBd.Unidad_Medida = datos.Unidad_Medida;
            productoBd.Ubicacion     = datos.Ubicacion;
            productoBd.Piezas        = datos.Piezas;
            productoBd.Stock_Minimo  = datos.Stock_Minimo;

            await _producto.GuardarAsync();

            return Ok(new {message = "Producto editado"});
        }

        [HttpPost("CambiarPrecio/{Id:int}")]
        public async Task<IActionResult> Cambiarprecio(int Id, DtoProductoUPrecio datos)
        {
            var productoBd = await _db.productos.Obtener(Id);
            if (productoBd is null) return NotFound();

            var ultimoHistorial = await _db.historialPrecios.GetUltimoPrecio(Id);

            var costo      = datos.Costo          ?? ultimoHistorial?.Costo          ?? productoBd.Costo;
            var precio     = datos.Precio          ?? ultimoHistorial?.Precio         ?? productoBd.Precio;
            var conversion = datos.ConversionABs   ?? ultimoHistorial?.ConversionABs  ?? productoBd.ConversionABs;

            var historial = productoBd.CambiarPrecio(costo, precio, conversion, datos.Nota);

            await _db.historialPrecios.Crear(historial);

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

            var margenLista = await _margen.GetUnico();
            var nuevosEnSesion = new Dictionary<(string, int?), Producto>();

            foreach (var item in Lista.Productos)
            {
                if (item.Precio == 0 && margenLista is not null)
                    item.Precio = CalcularPrecioConMargen(item.Costo * item.ConversionABs, margenLista.Valor);

                var key = (item.Codigo, item.MarcaId);
                bool esNuevoEnSesion = nuevosEnSesion.TryGetValue(key, out var tracked);
                var producto = esNuevoEnSesion
                    ? tracked
                    : await _db.productos.GetProductoforCodigo(item.Codigo, item.MarcaId);

                if (producto != null)
                {
                    if (!producto.Activo)
                    {
                        producto.Activo = true;
                        producto.FechaEliminacion = null;
                    }

                    if (producto.EsKit)
                    {
                        var cantidadNueva = item.Cantidad;
                        var preciocambio = item.Precio > 0 ? item.Precio : producto.Precio;
                        var costocambio = item.Costo > 0 ? item.Costo : producto.Costo;
                        var precio = producto.CambiarPrecio(costocambio, preciocambio, item.ConversionABs, "Actualizacion de la lista");
                        if (!esNuevoEnSesion) await _db.historialPrecios.Crear(precio);
                        if (cantidadNueva > 0)
                            producto.AgregarStockKit(cantidadNueva);
                    }
                    else
                    {
                        var historial = item.Actualizar(producto, "Actualizacion de la lista");
                        if (!esNuevoEnSesion) await _db.historialPrecios.Crear(historial);
                    }
                    ActualizadoCant++;
                }
                else
                {
                    var newproducto = item.Crear();
                    await _db.productos.Crear(newproducto);
                    nuevosEnSesion[key] = newproducto;
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
                var producto = await _db.productos.GetProductoforCodigo(item.Codigo, item.MarcaId);

                if (producto != null)
                {
                    if (!producto.Activo)
                    {
                        producto.Activo = true;
                        producto.FechaEliminacion = null;
                    }

                    if (producto.EsKit)
                    {
                        var cantidadNueva = item.Cantidad;
                        var preciocambio = item.Precio > 0 ? item.Precio : producto.Precio;
                        var costocambio = item.Costo > 0 ? item.Costo : producto.Costo;
                        var precio = producto.CambiarPrecio(costocambio, preciocambio, item.ConversionABs, "Actualizado por importacion");
                        await _db.historialPrecios.Crear(precio);
                        if (cantidadNueva > 0)
                            producto.AgregarStockKit(cantidadNueva);
                    }
                    else
                    {
                        var precio = item.Actualizar(producto, "Actualizado por importacion");
                        await _db.historialPrecios.Crear(precio);
                    }

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

        [HttpPut("ConvertirKit/{id:int}")]
        public async Task<IActionResult> ConvertirAKit(int id, DtoConvertirAKit datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.productos.ObtenerConPiezas(id);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });
            if (producto.EsKit) return BadRequest(new { message = "El producto ya es un kit." });

            var piezas = datos.Piezas.Select(p => p.Crear()).ToList();

            var codigos = piezas.Select(p => p.CodigoUniversal).ToList();
            if (codigos.Distinct().Count() != codigos.Count())
                return BadRequest(new { message = "Hay piezas con códigos duplicados en el kit." });

            producto.ConvertirAKit(piezas);
            await _db.SaveUnitWork();

            foreach (var pieza in producto.PiezasKit)
                pieza.ActualizarCodigo();
            await _db.SaveUnitWork();

            return Ok(new { message = "Producto convertido a kit." });
        }

        [HttpPut("ConvertirRegular/{id:int}")]
        public async Task<IActionResult> ConvertirARegular(int id, DtoConvertirARegular datos)
        {
            var producto = await _db.productos.ObtenerConPiezas(id);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });
            if (!producto.EsKit) return BadRequest(new { message = "El producto ya es regular." });

            var stockFinal = datos.StockManual ?? producto.CalcularStockKit();
            producto.ConvertirARegular(stockFinal);

            await _db.SaveUnitWork();

            return Ok(new { message = "Producto convertido a regular.", stockFinal });
        }

        [HttpPost("{id:int}/Piezas")]
        public async Task<IActionResult> AgregarPiezas(int id, DtoConvertirAKit datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var producto = await _db.productos.ObtenerConPiezas(id);
            if (producto is null) return NotFound(new { message = "Producto no encontrado." });
            if (!producto.EsKit) return BadRequest(new { message = "El producto no es un kit." });

            var nuevasPiezas = datos.Piezas.Select(p => p.Crear()).ToList();

            var codigosNuevos = nuevasPiezas.Select(p => p.CodigoUniversal).ToList();
            if (codigosNuevos.Distinct().Count() != codigosNuevos.Count())
                return BadRequest(new { message = "Hay piezas con códigos duplicados en el lote." });

            var stockActual = producto.CalcularStockKit();
            foreach (var pieza in nuevasPiezas)
                pieza.EstablecerStockInicial(stockActual);

            producto.PiezasKit.AddRange(nuevasPiezas);
            producto.Stock_Actual = producto.CalcularStockKit();
            await _db.SaveUnitWork();

            foreach (var pieza in nuevasPiezas)
                pieza.ActualizarCodigo();
            await _db.SaveUnitWork();

            return Created("", new { message = "Piezas agregadas." });
        }

        [HttpPut("{id:int}/Piezas/{piezaId:int}")]
        public async Task<IActionResult> ActualizarPieza(int id, int piezaId, DtoActualizarPieza datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var pieza = await _db.piezasKit.Obtener(piezaId);
            if (pieza.Id_Producto != id) return BadRequest(new { message = "La pieza no pertenece a este producto." });

            pieza.ActualizarDatos(datos.CodigoBase, datos.Nombre, datos.CantidadPorKit);

            if (datos.CantidadPorKit.HasValue)
            {
                var producto = await _db.productos.ObtenerConPiezas(id);
                if (producto is not null)
                {
                    var nuevoStock = producto.CalcularStockKit();
                    if (nuevoStock < producto.StockReservado)
                        return Conflict(new { message = $"No se puede cambiar la cantidad. El nuevo stock ({nuevoStock}) sería menor al stock reservado ({producto.StockReservado})." });
                    producto.Stock_Actual = nuevoStock;
                }
            }

            await _db.SaveUnitWork();

            return Ok(new { message = "Pieza actualizada." });
        }

        [HttpDelete("{id:int}/Piezas/{piezaId:int}")]
        public async Task<IActionResult> EliminarPieza(int id, int piezaId)
        {
            var pieza = await _db.piezasKit.Obtener(piezaId);
            if (pieza.Id_Producto != id) return BadRequest(new { message = "La pieza no pertenece a este producto." });

            await _db.piezasKit.Eliminar(piezaId);
            await _db.SaveUnitWork();

            return NoContent();
        }
    }
}
