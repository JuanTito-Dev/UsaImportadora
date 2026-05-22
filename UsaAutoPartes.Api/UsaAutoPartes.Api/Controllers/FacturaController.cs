using Microsoft.AspNetCore.Mvc;
using UsaAutoPartes.Application.IServicios;

namespace UsaAutoPartes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturaController(IFacturaExtractorServicio _extractor) : ControllerBase
    {
        [HttpPost("extraer")]
        public async Task<IActionResult> Extraer(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No se recibió ningún archivo." });

            var ext = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".pdf")
                return BadRequest(new { error = $"Formato no soportado: {ext}. Solo .xlsx, .xls o .pdf" });

            try
            {
                using var stream  = file.OpenReadStream();
                var excelBytes    = await _extractor.ExtraerProductosAsync(stream, file.FileName);
                var nombreSalida  = System.IO.Path.GetFileNameWithoutExtension(file.FileName) + "_limpio.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreSalida
                );
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
        }
    }
}
