using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using UsaAutoPartes.Application.IServicios;

namespace UsaAutoPartes.Infrastructure.Servicios.Processors
{
    public class FacturaExtractorServicio : IFacturaExtractorServicio
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public FacturaExtractorServicio(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _http   = httpClientFactory.CreateClient();
            ExcelPackage.License.SetNonCommercialOrganization("UsaAutoPartes");
        }

        public async Task<byte[]> ExtraerProductosAsync(Stream excelStream)
        {
            var filas    = LeerExcel(excelStream);
            var (headers, rows) = await ExtraerConIaAsync(filas);
            return GenerarExcel(headers, rows);
        }

        // ── LEER EXCEL ───────────────────────────────────────────────────────

        private List<List<string?>> LeerExcel(Stream stream)
        {
            var filas = new List<List<string?>>();

            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            for (int row = 1; row <= ws.Dimension.End.Row; row++)
            {
                var fila = new List<string?>();
                for (int col = 1; col <= ws.Dimension.End.Column; col++)
                {
                    var val = ws.Cells[row, col].Value;
                    fila.Add(val?.ToString()?.Replace("\n", " ").Replace("\r", " "));
                }
                filas.Add(fila);
            }

            return filas;
        }

        // ── LLAMAR A LA IA ───────────────────────────────────────────────────

        private async Task<(List<string> Headers, List<List<string>> Rows)> ExtraerConIaAsync(List<List<string?>> filas)
        {
            var (apiKey, baseUrl, modelo) = ObtenerConfigIA();

            var muestra = filas
                .Take(60)
                .Select((fila, i) => new {
                    fila   = i + 1,
                    celdas = fila.Select(c => c?.Length > 120 ? c[..120] : (c ?? "")).ToList()
                })
                .Where(f => f.celdas.Any(c => !string.IsNullOrEmpty(c)))
                .ToList();

            int nColumnas = muestra.Count > 0 ? muestra.Max(m => m.celdas.Count) : 0;

            var prompt = $$"""
                Sos un experto en procesar facturas de importación en cualquier idioma y formato.
                Te doy las primeras filas de un Excel de proveedor. El archivo tiene hasta {{nColumnas}} columnas.

                TAREA:
                1. Encontrá la fila que tiene los nombres de columna (encabezado de tabla).
                   Puede estar en cualquier idioma: inglés, español, chino, etc.
                2. Extraé TODAS las columnas del encabezado — la cantidad exacta que tiene, sin agregar ni quitar ninguna.
                3. Extraé SOLO las filas de productos reales.
                   Una fila es producto si tiene: código/referencia + descripción + al menos un número (cantidad o precio).
                4. Ignorá: nombre de empresa, dirección, datos de envío, notas, filas de totales, filas vacías.
                5. Limpiá los valores:
                   - Precios: "$ 18.47" -> "18.47", "18,47 USD" -> "18.47"
                   - Quitá comas de miles: "1,490.00" -> "1490.00"
                   - Quitá saltos de línea dentro de celdas
                6. Si la tabla aparece duplicada, deduplicá.
                7. Cada fila de producto debe tener EXACTAMENTE la misma cantidad de valores que columnas hay en headers.
                   Si una celda está vacía, poné "".

                Respondé ÚNICAMENTE con este JSON, sin texto extra, sin backticks, sin explicaciones:
                {"headers": ["col1", "col2", "col3", ...], "rows": [["val1", "val2", "val3", ...], ...]}

                Filas del Excel (número de fila + array de celdas):
                {{JsonConvert.SerializeObject(muestra)}}
                """;

            var body = new
            {
                model          = modelo,
                max_tokens     = 6000,
                messages       = new[] { new { role = "user", content = prompt } },
                response_format = new { type = "json_object" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _http.SendAsync(request);
            var raw      = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error de la IA ({response.StatusCode}): {raw}");

            var json  = JObject.Parse(raw);
            var texto = json["choices"]?[0]?["message"]?["content"]?.ToString()
                        ?? throw new Exception("La IA devolvió respuesta vacía.");

            return ParsearRespuestaIa(texto);
        }

        // ── PARSEAR RESPUESTA ────────────────────────────────────────────────

        private (List<string> Headers, List<List<string>> Rows) ParsearRespuestaIa(string texto)
        {
            texto = Regex.Replace(texto, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();
            texto = Regex.Replace(texto, @"^```json\s*", "");
            texto = Regex.Replace(texto, @"^```\s*", "");
            texto = Regex.Replace(texto, @"```$", "").Trim();

            var match = Regex.Match(texto, @"\{.*\}", RegexOptions.Singleline);
            if (!match.Success)
                throw new Exception($"No se encontró JSON en la respuesta. Contenido: {texto[..Math.Min(400, texto.Length)]}");

            var jObj = JObject.Parse(match.Value);

            if (jObj["headers"] == null || jObj["rows"] == null)
                throw new Exception($"JSON sin 'headers' o 'rows'. Claves: {string.Join(", ", jObj.Properties().Select(p => p.Name))}");

            var headers = jObj["headers"]!.Values<string>().Select(h => h ?? "").ToList();
            var rows    = jObj["rows"]!
                .Select(r => r.Values<string>().Select(v => v ?? "").ToList())
                .ToList();

            int n = headers.Count;
            var rowsNorm = rows.Select(fila =>
            {
                if (fila.Count < n) fila.AddRange(Enumerable.Repeat("", n - fila.Count));
                if (fila.Count > n) fila = fila.Take(n).ToList();
                return fila;
            }).ToList();

            return (headers, rowsNorm);
        }

        // ── GENERAR EXCEL LIMPIO ─────────────────────────────────────────────

        private byte[] GenerarExcel(List<string> headers, List<List<string>> rows)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Productos");

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Name = "Arial";
                cell.Style.Font.Size = 10;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 217, 217));
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < rows[r].Count; c++)
                {
                    var cell = ws.Cells[r + 2, c + 1];
                    cell.Value = rows[r][c];
                    cell.Style.Font.Name = "Arial";
                    cell.Style.Font.Size = 10;
                }
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns(8, 60);

            return package.GetAsByteArray();
        }

        // ── CONFIGURACIÓN IA ─────────────────────────────────────────────────

        private (string apiKey, string baseUrl, string modelo) ObtenerConfigIA()
        {
            var minimaxKey = _config["IA:MinimaxApiKey"];
            var grokKey    = _config["IA:GrokApiKey"];

            if (!string.IsNullOrEmpty(minimaxKey))
                return (minimaxKey, "https://api.minimax.io/v1", "MiniMax-M2");

            if (!string.IsNullOrEmpty(grokKey))
                return (grokKey, "https://api.x.ai/v1", "grok-3-mini");

            throw new Exception("No hay API key configurada. Agregá IA:MinimaxApiKey o IA:GrokApiKey en appsettings.json");
        }
    }
}
