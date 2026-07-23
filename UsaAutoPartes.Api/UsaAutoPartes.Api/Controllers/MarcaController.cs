using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsaAutoPartes.Application.IRepositorio;
using UsaAutoPartes.Domain.Entities;
using UsaAutoPartes.Domain.Enum.UsuarioEnums;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UsuarioRoles.Admin)]
    public class MarcaController(IMarcaRepositorio _marca) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var marcas = await _marca.Query().OrderBy(m => m.Nombre).ToListAsync();
            return Ok(marcas);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(DtoMarcaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nombre = datos.Nombre.Trim();
            var existe = await _marca.ObtenerPorNombre(nombre);
            if (existe is not null) return Conflict(new { message = "Ya existe una marca con ese nombre" });

            var prefijo = await GenerarPrefijoUnico(nombre);
            var marca = new Marca { Nombre = nombre, Prefijo = prefijo };

            await _marca.Crear(marca);
            await _marca.GuardarAsync();

            return Created("", new { message = "Marca creada", id = marca.Id, nombre = marca.Nombre, prefijo = marca.Prefijo });
        }

        private async Task<string> GenerarPrefijoUnico(string nombre)
        {
            var base_ = new string(
                nombre.Normalize(System.Text.NormalizationForm.FormD)
                      .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                  != System.Globalization.UnicodeCategory.NonSpacingMark
                               && char.IsLetter(c))
                      .ToArray()
            ).ToUpper();

            if (base_.Length < 3) base_ = base_.PadRight(3, '_');

            for (int len = 3; len <= base_.Length; len++)
            {
                var candidato = base_[..len];
                if (!await _marca.PrefijoExiste(candidato)) return candidato;
            }

            int n = 2;
            while (true)
            {
                var candidato = $"{base_}_{n}";
                if (!await _marca.PrefijoExiste(candidato)) return candidato;
                n++;
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, DtoMarcaCU datos)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var marca = await _marca.Obtener(id);

            marca.Nombre = datos.Nombre.Trim();

            await _marca.GuardarAsync();

            return Ok(new { message = "Marca actualizada" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _marca.Eliminar(id);
            await _marca.GuardarAsync();

            return Ok(new { message = "Marca eliminada" });
        }
    }

    public class DtoMarcaCU
    {
        [System.ComponentModel.DataAnnotations.Required]
        public required string Nombre { get; set; }
    }
}
