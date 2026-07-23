using System.Drawing;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
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
        private readonly AnthropicClient _claude;

        public FacturaExtractorServicio(IConfiguration config)
        {
            ExcelPackage.License.SetNonCommercialOrganization("UsaAutoPartes");
            var apiKey = config["IA:ClaudeApiKey"]
                ?? throw new InvalidOperationException("Falta IA:ClaudeApiKey en la configuración.");
            _claude = new AnthropicClient
            {
                ApiKey  = apiKey,
                Timeout = TimeSpan.FromMinutes(20)
            };
        }

        // ── ENTRY POINT ──────────────────────────────────────────────────────

        public async Task<byte[]> ExtraerProductosAsync(Stream fileStream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();

            List<ContentBlockParam> content;
            if (ext == ".pdf")
            {
                var bytes = LeerStreamComoBytes(fileStream);
                content   = BuildPdfContent(bytes);
            }
            else
            {
                var filas = LeerExcel(fileStream);
                content   = BuildExcelContent(filas);
            }

            var jsonRespuesta    = await LlamarClaudeAsync(content);
            var (headers, rows) = ParsearRespuesta(jsonRespuesta);
            return GenerarExcel(headers, rows);
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

        private static List<ContentBlockParam> BuildExcelContent(List<List<string?>> filas)
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

            var texto = $"""
                Analizá este archivo Excel de factura de proveedor. Tiene {nColumnas} columnas.

                Filas del Excel (número de fila + array de celdas):
                {JsonConvert.SerializeObject(muestra)}
                """;

            return [new ContentBlockParam(new TextBlockParam(texto))];
        }

        // ── PDF ────────────────────────────────────────────────────────────────

        private static byte[] LeerStreamComoBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static List<ContentBlockParam> BuildPdfContent(byte[] pdfBytes)
        {
            var b64 = Convert.ToBase64String(pdfBytes);
            return
            [
                new ContentBlockParam(new DocumentBlockParam(new Base64PdfSource { Data = b64 })),
                new ContentBlockParam(new TextBlockParam("Analizá este PDF de factura de proveedor y extraé la tabla de productos."))
            ];
        }

        // ── CLAUDE API ────────────────────────────────────────────────────────

        private const string SystemPrompt = """
            Sos un experto en extraer datos de facturas de importación de proveedores extranjeros.

            TAREA: Extraé SOLO la tabla de productos/items del documento.

            REGLAS CRÍTICAS:
            1. Encontrá la fila que contiene los nombres de columna reales. Puede estar en cualquier idioma (inglés, español, chino, portugués).
            2. IGNORÁ columnas completamente vacías (spacers). Incluí en headers SOLO las columnas que tienen al menos un valor en las filas de producto.
            3. Si el documento tiene múltiples páginas, los headers pueden repetirse. Usá SOLO el primer set de headers.
            4. Extraé ÚNICAMENTE filas de productos reales. Una fila es producto si tiene: código/referencia + al menos un número (cantidad o precio). Descripción es opcional — algunos proveedores solo ponen código y precio.
            5. IGNORÁ completamente: nombre de empresa, dirección, datos de envío, teléfonos, totales, subtotales, notas de pago, datos bancarios, filas vacías, footers, balance due, términos de pago, filas de pallets/embalaje.
            6. Limpiá valores numéricos:
            - Precios: "$ 18.47" → "18.47", "18,47 USD" → "18.47", "$ 1,490.00" → "1490.00"
            - Eliminá separadores de miles: "1,490.00" → "1490.00"
            - Eliminá saltos de línea dentro de celdas
            - NO recalculés ni redondeés — transcribí el valor tal cual, limpio de símbolos.
            7. Si hay filas duplicadas (por saltos de página), deduplicá.
            8. Cada fila debe tener EXACTAMENTE la misma cantidad de valores que columnas en headers. Celda vacía → "".

            9. PROVEEDOR: Buscá el nombre del proveedor/empresa emisora en el encabezado ("Vendor:", "Supplier:", "From:", nombre de empresa, etc.).
            - Si lo encontrás con certeza, agregá columna "Proveedor" con ese valor repetido en cada fila.
            - Si no estás seguro, no agregues la columna.

            10. NOMBRE Y DESCRIPCIÓN:
                - Si el documento ya tiene columnas SEPARADAS para nombre corto (ej: "Line", "Type", "Tipo") y descripción larga: respetá ambas columnas tal cual, sin modificar ni fusionar.
                - Si hay UNA SOLA columna que mezcla nombre corto + descripción técnica larga (vehículos, motores, años, medidas): separala en dos columnas: "Nombre" con las primeras 2-4 palabras que identifican el tipo de parte (ej: "METAL DE BIELA", "METALES DE CENTRO", "TIMING CHAIN KIT") y "Descripción" con el resto. El corte va antes del primer vehículo compatible, marca de vehículo, medida de motor o especificación técnica.
                - Si no hay descripción de texto (solo código y precio): no crees columna Nombre ni Descripción vacías.

            11. MARCA: Agregá columna "Marca" SOLO si la marca del fabricante de la PARTE viene explícita:
                - En el encabezado del documento con etiqueta clara (ej: "Brand: CIC", "Marca: BOSCH").
                - O en una columna propia de la tabla.
                - NUNCA extraigas marca de nombres de vehículos compatibles (Ford, Chrysler, Nissan, Jeep, etc.) — esos son vehículos, no marcas de la parte.
                - Si la marca está en el encabezado, repetila en cada fila de producto.

            12. PROCEDENCIA: Agregá columna "Procedencia" en estos casos:
                - Si hay una columna por producto con país de origen (ej: "Origen", "COO", "Country of Origin"): incluila tal cual, expandiendo códigos ISO de 2 letras a nombre completo del país (CN→China, MX→Mexico, IN→India, US→USA, TR→Turquía, BR→Brasil, AR→Argentina, TW→Taiwán, IT→Italia, DE→Alemania, JP→Japón, KR→Corea del Sur).
                - Si hay un dato de procedencia único para todo el documento (ej: "FROM: NINGBO, CHINA", "Made in China", "Origin: USA"): extraé el país y repetilo en cada fila.
                - La dirección o ciudad del EMISOR/VENDEDOR NO es procedencia de la mercancía.
                - Si no hay procedencia explícita de la mercancía, no crees la columna.
            """;

        private static readonly Dictionary<string, JsonElement> _outputSchema =
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
                {
                    "type": "object",
                    "properties": {
                        "headers": { "type": "array", "items": { "type": "string" } },
                        "rows": {
                            "type": "array",
                            "items": { "type": "array", "items": { "type": "string" } }
                        }
                    },
                    "required": ["headers", "rows"],
                    "additionalProperties": false
                }
                """)!;

        private async Task<string> LlamarClaudeAsync(List<ContentBlockParam> content)
        {
            var response = await _claude.Messages.Create(new MessageCreateParams
            {
                Model     = "claude-sonnet-4-6",
                MaxTokens = 8192,
                System    = new MessageCreateParamsSystem(new List<TextBlockParam>
                {
                    new TextBlockParam
                    {
                        Text         = SystemPrompt,
                        CacheControl = new CacheControlEphemeral()
                    }
                }),
                OutputConfig = new OutputConfig
                {
                    Format = new JsonOutputFormat { Schema = _outputSchema }
                },
                Messages =
                [
                    new()
                    {
                        Role    = Role.User,
                        Content = new MessageParamContent(content)
                    }
                ]
            });

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var textBlock))
                    return textBlock.Text;
            }

            throw new Exception("Claude no devolvió contenido de texto.");
        }

        // ── PARSEAR RESPUESTA ────────────────────────────────────────────────

        private static (List<string> Headers, List<List<string>> Rows) ParsearRespuesta(string json)
        {
            var jObj = JObject.Parse(json);

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
    }
}
