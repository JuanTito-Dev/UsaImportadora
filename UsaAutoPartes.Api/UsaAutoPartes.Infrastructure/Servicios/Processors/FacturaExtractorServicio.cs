using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using UglyToad.PdfPig;
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

        public async Task<byte[]> ExtraerProductosAsync(Stream fileStream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            var (geminiKey, groqKey) = ObtenerKeys();

            List<string> headers;
            List<List<string>> rows;

            if (ext == ".pdf")
            {
                var pdfBytes = LeerStreamComoBytes(fileStream);
                (headers, rows) = await ConFallbackAsync(
                    geminiKey != null ? () => ExtraerDePdfConGeminiAsync(pdfBytes, geminiKey) : null,
                    groqKey   != null ? () => ExtraerDePdfConGrokAsync(pdfBytes, groqKey, "llama-3.3-70b-versatile") : null
                );
            }
            else
            {
                var filas = LeerExcel(fileStream);
                (headers, rows) = await ConFallbackAsync(
                    geminiKey != null ? () => ExtraerDeExcelConGeminiAsync(filas, geminiKey) : null,
                    groqKey   != null ? () => ExtraerDeExcelConGrokAsync(filas, groqKey, "llama-3.3-70b-versatile") : null
                );
            }

            return GenerarExcel(headers, rows);
        }

        private static async Task<(List<string>, List<List<string>>)> ConFallbackAsync(
            Func<Task<(List<string>, List<List<string>>)>>? primario,
            Func<Task<(List<string>, List<List<string>>)>>? fallback)
        {
            if (primario == null && fallback == null)
                throw new Exception("No hay API key configurada. Agregá IA:GeminiApiKey o IA:GrokApiKey en la configuración.");

            if (primario != null)
            {
                try   { return await primario(); }
                catch { if (fallback == null) throw; }
            }

            return await fallback!();
        }

        // ── EXCEL ─────────────────────────────────────────────────────────────

        private static List<List<string?>> LeerExcel(Stream stream)
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

        private static string BuildPromptExcel(List<List<string?>> filas)
        {
            var muestra = filas
                .Take(60)
                .Select((fila, i) => new {
                    fila   = i + 1,
                    celdas = fila.Select(c => c?.Length > 120 ? c[..120] : (c ?? "")).ToList()
                })
                .Where(f => f.celdas.Any(c => !string.IsNullOrEmpty(c)))
                .ToList();

            int nColumnas = muestra.Count > 0 ? muestra.Max(m => m.celdas.Count) : 0;

            return $$"""
                Sos un experto en extraer datos de facturas de importación de proveedores extranjeros.
                Te doy filas crudas de un archivo Excel (número de fila + array de celdas). El archivo tiene {{nColumnas}} columnas.

                REGLAS CRÍTICAS:
                1. Encontrá la fila que contiene los nombres de columna reales. Puede estar en cualquier idioma (inglés, español, chino, portugués).
                2. IGNORÁ columnas completamente vacías (spacers). Incluí en headers SOLO las columnas que tienen al menos un valor en las filas de producto.
                3. Si el documento tiene múltiples páginas, los headers pueden repetirse. Usá SOLO el primer set de headers.
                4. Extraé ÚNICAMENTE filas de productos reales. Una fila es producto si tiene: código/referencia + descripción + al menos un número (cantidad o precio).
                5. IGNORÁ completamente: nombre de empresa, dirección, datos de envío, teléfonos, totales, subtotales, notas de pago, datos bancarios, filas vacías, footers, balance due, términos de pago.
                6. Limpiá valores:
                   - Precios: "$ 18.47" → "18.47", "18,47 USD" → "18.47", "$ 1,490.00" → "1490.00"
                   - Eliminá comas de miles: "1,490.00" → "1490.00"
                   - Eliminá saltos de línea dentro de celdas
                7. Si hay filas duplicadas (por saltos de página), deduplicá.
                8. Cada fila debe tener EXACTAMENTE la misma cantidad de valores que columnas en headers. Celda vacía → "".

                Respondé ÚNICAMENTE con este JSON, sin texto extra, sin backticks:
                {"headers":["col1","col2"],"rows":[["val1","val2"]]}

                Filas del Excel:
                {{JsonConvert.SerializeObject(muestra)}}
                """;
        }

        private async Task<(List<string>, List<List<string>>)> ExtraerDeExcelConGeminiAsync(
            List<List<string?>> filas, string apiKey)
        {
            var texto = await LlamarGeminiAsync(apiKey, BuildPromptExcel(filas));
            return ParsearRespuestaIa(texto);
        }

        private async Task<(List<string>, List<List<string>>)> ExtraerDeExcelConGrokAsync(
            List<List<string?>> filas, string apiKey, string modelo)
        {
            var texto = await LlamarGrokAsync(apiKey, modelo, BuildPromptExcel(filas));
            return ParsearRespuestaIa(texto);
        }

        // ── PDF ────────────────────────────────────────────────────────────────

        private static byte[] LeerStreamComoBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private const string PromptPdf = """
            Sos un experto en extraer datos de facturas de importación. Analizá este PDF de factura de proveedor.

            TAREA: Extraé SOLO la tabla de productos/items del documento.

            REGLAS:
            1. Identificá las columnas reales de la tabla de items (código, descripción, cantidad, precio unitario, total, etc.)
            2. IGNORÁ: membrete de empresa, logo, dirección, datos de envío/cliente, totales finales, notas de pago, datos bancarios, balance due, términos de pago.
            3. Si la tabla se repite en múltiples páginas, combiná todos los items en una sola lista sin duplicar los headers.
            4. Limpiá precios: "$ 18.47" → "18.47", eliminá símbolos de moneda y comas de miles.
            5. Cada fila de producto debe tener exactamente la misma cantidad de valores que columnas. Celda vacía → "".

            Respondé ÚNICAMENTE con este JSON, sin texto extra, sin backticks:
            {"headers":["col1","col2"],"rows":[["val1","val2"]]}
            """;

        private async Task<(List<string>, List<List<string>>)> ExtraerDePdfConGeminiAsync(
            byte[] pdfBytes, string apiKey)
        {
            var texto = await LlamarGeminiConPdfAsync(apiKey, pdfBytes, PromptPdf);
            return ParsearRespuestaIa(texto);
        }

        private async Task<(List<string>, List<List<string>>)> ExtraerDePdfConGrokAsync(
            byte[] pdfBytes, string apiKey, string modelo)
        {
            var textoPdf = LeerPdfComoTexto(pdfBytes);
            if (textoPdf.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 30)
                throw new Exception("El PDF parece ser escaneado (imagen sin texto). Convertilo a Excel o usá un PDF con texto seleccionable.");

            var texto = await LlamarGrokAsync(apiKey, modelo, PromptPdf + "\n\nContenido del PDF:\n" + textoPdf);
            return ParsearRespuestaIa(texto);
        }

        private static string LeerPdfComoTexto(byte[] bytes)
        {
            var sb = new StringBuilder();
            using var pdf = PdfDocument.Open(bytes);
            foreach (var page in pdf.GetPages())
                sb.AppendLine(string.Join(" ", page.GetWords().Select(w => w.Text)));
            return sb.ToString();
        }

        // ── GEMINI API (nativa v1) ─────────────────────────────────────────────

        private async Task<string> LlamarGeminiAsync(string apiKey, string prompt)
        {
            var url  = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";
            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { maxOutputTokens = 16384 }
            };
            return await EnviarGeminiAsync(url, body);
        }

        private async Task<string> LlamarGeminiConPdfAsync(string apiKey, byte[] pdfBytes, string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";
            var b64 = Convert.ToBase64String(pdfBytes);
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inlineData = new { mimeType = "application/pdf", data = b64 } },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new { maxOutputTokens = 16384 }
            };
            return await EnviarGeminiAsync(url, body);
        }

        private async Task<string> EnviarGeminiAsync(string url, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);
            var raw      = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error de Gemini ({response.StatusCode}): {raw}");

            try
            {
                var json  = JObject.Parse(raw);
                var texto = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
                            ?? throw new Exception("Gemini devolvió respuesta vacía.");
                return texto;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error al parsear respuesta de Gemini: {ex.Message}. Raw: {raw[..Math.Min(500, raw.Length)]}");
            }
        }

        // ── GROQ API (OpenAI-compatible) ────────────────────────────────────────

        private async Task<string> LlamarGrokAsync(string apiKey, string modelo, string prompt)
        {
            var body = new
            {
                model           = modelo,
                max_tokens      = 16384,
                messages        = new[] { new { role = "user", content = prompt } },
                response_format = new { type = "json_object" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _http.SendAsync(request);
            var raw      = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error de Groq ({response.StatusCode}): {raw}");

            var json  = JObject.Parse(raw);
            var texto = json["choices"]?[0]?["message"]?["content"]?.ToString()
                        ?? throw new Exception("Groq devolvió respuesta vacía.");
            return texto;
        }

        // ── PARSEAR RESPUESTA ────────────────────────────────────────────────

        private static (List<string> Headers, List<List<string>> Rows) ParsearRespuestaIa(string texto)
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

        private static byte[] GenerarExcel(List<string> headers, List<List<string>> rows)
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

        private (string? geminiKey, string? groqKey) ObtenerKeys()
        {
            var gemini = _config["IA:GeminiApiKey"];
            var groq   = _config["IA:GroqApiKey"];
            return (
                string.IsNullOrEmpty(gemini) ? null : gemini,
                string.IsNullOrEmpty(groq)   ? null : groq
            );
        }
    }
}
