using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JewerlySystem.Common.Application.Voice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Common.Infrastructure.Voice;

public class GoogleGeminiVoiceService : IVoiceInterpreterService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleGeminiVoiceService> _logger;

    public GoogleGeminiVoiceService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleGeminiVoiceService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<VoiceOperationResult> InterpretAsync(string transcription, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcription))
        {
            return new VoiceOperationResult
            {
                Intent = VoiceIntents.Desconocido,
                Summary = "No se recibió texto de dictado.",
                RawTranscription = string.Empty
            };
        }

        string? apiKey = _configuration["GoogleGemini:ApiKey"];

        // Si no hay API Key configurada o tiene el placeholder, usamos el parser heurístico de respaldo
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("GoogleGemini:ApiKey no está configurada en appsettings.json. Usando analizador heurístico local.");
            return ParseHeuristically(transcription);
        }

        try
        {
            var prompt = $$"""
                Eres un asistente de inteligencia artificial para el sistema de gestión de una joyería y taller ('JewelrySystem').
                Tu función es analizar la transcripción de una nota de voz dictada por el usuario y extraer la intención comercial y sus datos estructurados en formato JSON.

                TRANSCRIPCIÓN DICTADA:
                "{{transcription}}"

                INTENCIONES POSIBLES:
                1. "CREAR_VENTA": Para registrar la venta de un artículo del catálogo de inventario (joya o reloj) que debe ser descargado del stock activo.
                   Campos en 'sale':
                   - reference (número entero, OBLIGATORIO: número de referencia del artículo en inventario, ej: 1042, 1001)
                   - saleValue (número entero, OBLIGATORIO: valor definitivo acordado en pesos colombianos)
                   - description (string opcional, descripción de la joya o reloj vendido)
                   - customerName (string opcional, nombre del comprador)
                   - customerIdentification (string opcional, cédula o documento del comprador)
                   - paymentMethod (string: 'EFECTIVO', 'TRANSFERENCIA', 'TARJETA')

                   ⚠️ REGLA ESTRICTA DE NEGOCIO:
                   Para descargar el artículo del sistema, el NÚMERO DE REFERENCIA ES ESTRICTAMENTE OBLIGATORIO. Si el usuario indica una venta pero NO dice el número de referencia, DEBES incluir obligatoriamente "Número de referencia del artículo (requerido para descargarlo de inventario)" en 'missingFields'.

                   EJEMPLO DISCURSO DE VENTA COMPLETA:
                   Dictado: "Venta de cadena de oro referencia 1042 por valor de 450000 pesos en efectivo al cliente Carlos Ruiz con cédula 1020304050"
                   Extracción:
                   {
                     "intent": "CREAR_VENTA",
                     "confidence": 0.98,
                     "summary": "Venta y descarga de ref #1042 por $450,000 en EFECTIVO a Carlos Ruiz (C.C. 1020304050)",
                     "sale": {
                       "reference": 1042,
                       "saleValue": 450000,
                       "description": "Venta de cadena de oro ref 1042",
                       "customerName": "Carlos Ruiz",
                       "customerIdentification": "1020304050",
                       "paymentMethod": "EFECTIVO"
                     },
                     "missingFields": []
                   }

                2. "CREAR_CONTRATO": Para contratos de hechura, reparación o separación de joyas.
                   Campos requeridos en 'contract':
                   - customerName (string, nombre del cliente)
                   - customerIdentification (string opcional, ej: cédula)
                   - description (string, detalle de la joya, ej: 'Anillo de oro 18k con esmeralda')
                   - value (número entero, valor total acordado en pesos colombianos)
                   - initialPayment (número entero, abono inicial realizado, o 0 si no dio)
                   - weight (número decimal, peso en gramos si se menciona, ej: 4.5, o 0)
                   - deliveryDays (número entero, días estimados para entrega, default 15)
                   - paymentMethod (string: 'EFECTIVO', 'TRANSFERENCIA', 'TARJETA')

                3. "CREAR_CLIENTE": Para registrar una nueva persona o cliente.
                   Campos en 'customer':
                   - fullName (string, nombre completo)
                   - identificationNumber (string opcional, número de cédula o NIT)
                   - cellPhone (string opcional, número de celular)
                   - email (string opcional)

                4. "CREAR_MOVIMIENTO_CAJA": Para ingresos o egresos directos de caja que NO corresponden a la venta de un artículo con referencia (ej: pago de servicios, compra de insumos, ajuste de caja).
                   Campos en 'cash':
                   - description (string, concepto de la venta o movimiento)
                   - value (número entero, valor en pesos)
                   - recordType (string: 'INGRESO' para entradas, 'EGRESO' para gastos)
                   - paymentMethod (string: 'EFECTIVO', 'TRANSFERENCIA', 'TARJETA')

                5. "CREAR_FACTURA": Para registrar facturas de proveedores o compras.
                   Campos en 'invoice':
                   - providerName (string, nombre del proveedor o taller)
                   - value (número entero, valor total)
                   - creditValue (número entero, abono inicial a la factura)
                   - description (string)

                6. "DESCONOCIDO": Si lo dictado no corresponde a ninguna operación del sistema.

                Responde OBLIGATORIAMENTE un único objeto JSON válido con este formato:
                {
                  "intent": "CREAR_VENTA" | "CREAR_CONTRATO" | "CREAR_CLIENTE" | "CREAR_MOVIMIENTO_CAJA" | "CREAR_FACTURA" | "DESCONOCIDO",
                  "confidence": 0.95,
                  "summary": "Resumen claro en 1 oración de la operación detectada",
                  "sale": { ... } o null,
                  "contract": { ... } o null,
                  "customer": { ... } o null,
                  "cash": { ... } o null,
                  "invoice": { ... } o null,
                  "missingFields": ["lista de campos obligatorios faltantes, si los hay"]
                }
                """;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };

            var model = _configuration["GoogleGemini:Model"] ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Error al invocar Google Gemini API: {StatusCode} - {Error}", response.StatusCode, errorBody);
                
                string notice = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "La API de Gemini requiere activar facturación en Google Cloud (Error 403). Usando analizador inteligente local."
                    : $"Servicio Gemini no disponible ({response.StatusCode}). Usando analizador inteligente local.";

                return ParseHeuristically(transcription, notice);
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: ct);
            var text = jsonResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return ParseHeuristically(transcription, "Gemini no devolvió texto. Fallback local ejecutado.");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var operation = JsonSerializer.Deserialize<VoiceOperationResult>(text, options);

            if (operation != null)
            {
                operation.RawTranscription = transcription;
                operation.RawJson = text;
                operation.EngineNotice = $"Procesado con Inteligencia Artificial Google Gemini ({model})";

                if (IsDemoExampleText(transcription))
                {
                    operation.IsFromExample = true;
                    operation.EngineNotice = "Modo Demostración: Los ejemplos de prueba no se pueden guardar en la base de datos.";
                }

                return operation;
            }

            return ParseHeuristically(transcription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al procesar comando de voz con Gemini.");
            return ParseHeuristically(transcription, $"Error de conexión con Gemini: {ex.Message}. Fallback local ejecutado.");
        }
    }

    /// <summary>
    /// Detecta si el texto dictado corresponde a uno de los ejemplos predeterminados de prueba.
    /// </summary>
    public static bool IsDemoExampleText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim();
        return t.Contains("Carlos Ruiz con cédula 1020304050", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Juan Carlos Pérez por 1800000", StringComparison.OrdinalIgnoreCase)
            || t.Contains("María Fernanda Gómez con cédula 10203040", StringComparison.OrdinalIgnoreCase)
            || t.Contains("compra de suministros de taller", StringComparison.OrdinalIgnoreCase)
            || (t.Contains("cadena de oro", StringComparison.OrdinalIgnoreCase) && t.Contains("1042", StringComparison.OrdinalIgnoreCase) && t.Contains("450000", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parser heurístico local estricto para cuando no hay conexión con Gemini.
    /// No utiliza valores por defecto fácticos ni inventados.
    /// </summary>
    private static VoiceOperationResult ParseHeuristically(string text, string? notice = null)
    {
        var lower = text.ToLowerInvariant();
        var isDemo = IsDemoExampleText(text);

        var result = new VoiceOperationResult
        {
            RawTranscription = text,
            Confidence = 0.85,
            IsFromExample = isDemo,
            EngineNotice = isDemo
                ? "Modo Demostración: Los ejemplos de prueba no se pueden guardar en la base de datos."
                : (notice ?? "Procesado con analizador inteligente local")
        };

        // 1. Venta de artículo en inventario (requiere referencia obligatoria)
        if (lower.Contains("venta") || lower.Contains("vender") || lower.Contains("descargar") || lower.Contains("descargue"))
        {
            result.Intent = VoiceIntents.CrearVenta;
            var refNum = ExtractReferenceNumber(text);
            var (val, _) = ExtractMonetaryValues(text, refNum);
            var custName = ExtractCustomerName(text);
            var custId = ExtractCustomerDoc(text);

            result.Sale = new VoiceSalePayload
            {
                Reference = refNum,
                SaleValue = val,
                Description = text,
                CustomerName = custName,
                CustomerIdentification = custId,
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : (lower.Contains("tarjeta") ? "TARJETA" : "EFECTIVO")
            };

            if (refNum <= 0)
            {
                result.MissingFields.Add("Número de referencia del artículo en inventario");
            }

            if (val <= 0)
            {
                result.MissingFields.Add("Valor definitivo de la venta en pesos");
            }

            if (result.MissingFields.Any())
            {
                result.Summary = $"Venta detectada{(refNum > 0 ? $" para ref #{refNum}" : "")}{(val > 0 ? $" por ${val:N0}" : "")}, pero faltan datos: {string.Join(", ", result.MissingFields)}.";
            }
            else
            {
                var buyerInfo = !string.IsNullOrEmpty(custName) ? $" a {custName}" : string.Empty;
                var idInfo = !string.IsNullOrEmpty(custId) ? $" (C.C. {custId})" : string.Empty;
                result.Summary = $"Venta y descarga de ref #{refNum} por ${(result.Sale.SaleValue):N0} en {result.Sale.PaymentMethod}{buyerInfo}{idInfo}.";
            }
        }
        // 2. Contrato
        else if (lower.Contains("contrato") || lower.Contains("hechura") || lower.Contains("reparación") || lower.Contains("reparacion") || lower.Contains("separar") || lower.Contains("separación"))
        {
            result.Intent = VoiceIntents.CrearContrato;
            var refNum = ExtractReferenceNumber(text);
            var (value, initial) = ExtractMonetaryValues(text, refNum);
            var customerName = ExtractCustomerName(text);
            var customerId = ExtractCustomerDoc(text);

            result.Contract = new VoiceContractPayload
            {
                CustomerName = customerName,
                CustomerIdentification = customerId,
                Description = text,
                Value = value,
                InitialPayment = initial,
                Weight = ExtractWeight(text),
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : "EFECTIVO"
            };

            if (string.IsNullOrWhiteSpace(customerName))
            {
                result.MissingFields.Add("Nombre del cliente");
            }

            if (value <= 0)
            {
                result.MissingFields.Add("Valor total del contrato");
            }

            if (result.MissingFields.Any())
            {
                result.Summary = $"Contrato detectado{(value > 0 ? $" por ${value:N0}" : "")}, pero faltan datos: {string.Join(", ", result.MissingFields)}.";
            }
            else
            {
                var abonoStr = initial > 0 ? $" con abono de ${initial:N0}" : " sin abono inicial";
                result.Summary = $"Contrato para {result.Contract.CustomerName} por ${(result.Contract.Value):N0}{abonoStr}.";
            }
        }
        // 3. Cliente
        else if (lower.Contains("cliente") || lower.Contains("persona") || lower.Contains("registrar cliente") || lower.Contains("nuevo cliente"))
        {
            result.Intent = VoiceIntents.CrearCliente;
            var fullName = ExtractCustomerName(text);
            var custDoc = ExtractCustomerDoc(text);
            var custPhone = ExtractCustomerPhone(text);

            result.Customer = new VoiceCustomerPayload
            {
                FullName = fullName ?? string.Empty,
                IdentificationNumber = custDoc,
                CellPhone = custPhone
            };

            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.MissingFields.Add("Nombre completo del cliente");
                result.Summary = "Registro de cliente detectado, pero falta indicar el nombre.";
            }
            else
            {
                var docInfo = !string.IsNullOrEmpty(custDoc) ? $" con documento {custDoc}" : "";
                var phoneInfo = !string.IsNullOrEmpty(custPhone) ? $" y celular {custPhone}" : "";
                result.Summary = $"Registrar cliente {fullName}{docInfo}{phoneInfo}.";
            }
        }
        // 4. Movimiento de Caja (Ingreso o Egreso general)
        else if (lower.Contains("caja") || lower.Contains("ingreso") || lower.Contains("gasto") || lower.Contains("egreso"))
        {
            result.Intent = VoiceIntents.CrearMovimientoCaja;
            var (val, _) = ExtractMonetaryValues(text);
            var isEgreso = lower.Contains("gasto") || lower.Contains("egreso") || lower.Contains("pago de servicio") || lower.Contains("compra de");

            result.Cash = new VoiceCashPayload
            {
                Description = text,
                Value = val,
                RecordType = isEgreso ? "EGRESO" : "INGRESO",
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : (lower.Contains("tarjeta") ? "TARJETA" : "EFECTIVO")
            };

            if (val <= 0)
            {
                result.MissingFields.Add("Monto o valor del movimiento");
                result.Summary = $"{(isEgreso ? "Egreso" : "Ingreso")} de caja detectado, pero falta especificar el valor.";
            }
            else
            {
                result.Summary = $"{(isEgreso ? "Egreso" : "Ingreso")} de caja por ${(result.Cash.Value):N0} en {result.Cash.PaymentMethod}.";
            }
        }
        else
        {
            result.Intent = VoiceIntents.Desconocido;
            result.Summary = "No se detectó una operación comercial clara (venta, contrato, cliente o movimiento de caja).";
            result.MissingFields.Add("Operación no identificada");
        }

        return result;
    }

    private static (int Total, int Initial) ExtractMonetaryValues(string text, int excludedReference = 0)
    {
        var cleanText = text;

        // 1. Enmascarar la referencia para que su número jamás se interprete como valor monetario
        if (excludedReference > 0)
        {
            cleanText = Regex.Replace(cleanText, $@"(?:referencia|ref\.?|código|codigo|joya|reloj|artículo|articulo|#)\s*#?\s*{excludedReference}\b", " [REF] ", RegexOptions.IgnoreCase);
            cleanText = Regex.Replace(cleanText, $@"\b{excludedReference}\b", " [REF] ");
        }

        // 2. Enmascarar documentos de identidad (cédulas de 6 a 11 dígitos)
        cleanText = Regex.Replace(cleanText, @"(?:cédula|cedula|cc|c\.c\.|nit|documento)\s*(?:de|no\.?|número|numero)?\s*(\d{6,11})\b", " [DOC] ", RegexOptions.IgnoreCase);

        // 3. Enmascarar números telefónicos
        cleanText = Regex.Replace(cleanText, @"(?:celular|cel|teléfono|telefono|tel|whatsapp)\s*(?:no\.?|número|numero)?\s*(\d{7,11})\b", " [TEL] ", RegexOptions.IgnoreCase);
        cleanText = Regex.Replace(cleanText, @"\b3\d{9}\b", " [TEL] ");

        var total = 0;
        var initial = 0;

        // Detectar millones (ej: "2 millones", "1 millón 500 mil")
        var millonMatches = Regex.Matches(cleanText, @"(\d+)\s*(?:millón|millon|millones)", RegexOptions.IgnoreCase);
        foreach (Match m in millonMatches)
        {
            if (int.TryParse(m.Groups[1].Value, out var val))
            {
                var amount = val * 1_000_000;
                var idx = m.Index;
                var preceding = cleanText.Substring(Math.Max(0, idx - 25), Math.Min(25, idx)).ToLowerInvariant();
                if (preceding.Contains("abono") || preceding.Contains("anticipo"))
                {
                    initial = amount;
                }
                else
                {
                    total = amount;
                }
            }
        }

        // Detectar miles (ej: "450 mil", "abono de 50 mil")
        var milMatches = Regex.Matches(cleanText, @"(\d+)\s*(?:mil)\b", RegexOptions.IgnoreCase);
        foreach (Match m in milMatches)
        {
            if (int.TryParse(m.Groups[1].Value, out var val))
            {
                var amount = val * 1_000;
                var idx = m.Index;
                var preceding = cleanText.Substring(Math.Max(0, idx - 25), Math.Min(25, idx)).ToLowerInvariant();
                if (preceding.Contains("abono") || preceding.Contains("anticipo"))
                {
                    initial = amount;
                }
                else if (total == 0)
                {
                    total = amount;
                }
                else if (total > 0 && initial == 0)
                {
                    var lastMillonIdx = cleanText.LastIndexOf("millon", idx, StringComparison.OrdinalIgnoreCase);
                    if (lastMillonIdx >= 0 && (idx - lastMillonIdx) < 25)
                    {
                        total += amount;
                    }
                    else
                    {
                        initial = amount;
                    }
                }
            }
        }

        // Detectar montos crudos con formato o dígitos (ej: "450000", "$450.000", "450.000")
        var rawMatches = Regex.Matches(cleanText, @"(?:\$?\s*)(\d{1,3}(?:\.\d{3})+|\d{4,9})\b");
        foreach (Match match in rawMatches)
        {
            var raw = match.Groups[1].Value.Replace(".", "");
            if (int.TryParse(raw, out var n))
            {
                if (n <= 0) continue;
                if (excludedReference > 0 && n == excludedReference) continue;

                var idx = match.Index;
                var preceding = cleanText.Substring(Math.Max(0, idx - 25), Math.Min(25, idx)).ToLowerInvariant();
                if (preceding.Contains("abono") || preceding.Contains("anticipo"))
                {
                    if (initial == 0) initial = n;
                }
                else
                {
                    if (total == 0) total = n;
                    else if (initial == 0 && n < total) initial = n;
                }
            }
        }

        return (total, initial);
    }

    private static double ExtractWeight(string text)
    {
        var match = Regex.Match(text, @"(\d+(?:[\.,]\d+)?)\s*(?:gramos|gr|g)\b", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var str = match.Groups[1].Value.Replace(",", ".");
            if (double.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, out var w))
                return w;
        }
        return 0;
    }

    private static string? ExtractCustomerName(string text)
    {
        var match = Regex.Match(text, @"(?:cliente|para|señor|señora|don|doña|a nombre de)\s+([A-ZÁÉÍÓÚÑa-záéíóúñ]+(?:\s+[A-ZÁÉÍÓÚÑa-záéíóúñ]+){1,3})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var name = match.Groups[1].Value.Trim();
            name = Regex.Replace(name, @"\b(con|cédula|cedula|cc|telefono|celular|por|para)\b.*$", "", RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        return null;
    }

    private static string? ExtractCustomerDoc(string text)
    {
        var match = Regex.Match(text, @"(?:cédula|cedula|cc|c\.c\.|nit|documento)\s*(?:de|no\.?|número|numero)?\s*(\d{6,11})\b", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        var digits = Regex.Matches(text, @"\b(\d{7,10})\b");
        foreach (Match d in digits)
        {
            var v = d.Groups[1].Value;
            if (!v.StartsWith("3")) return v;
        }
        return null;
    }

    private static string? ExtractCustomerPhone(string text)
    {
        var match = Regex.Match(text, @"(?:celular|cel|teléfono|telefono|tel|whatsapp)\s*(?:no\.?|número|numero)?\s*(\d{7,11})\b", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        var mobMatch = Regex.Match(text, @"\b(3\d{9})\b");
        if (mobMatch.Success) return mobMatch.Groups[1].Value;

        return null;
    }

    private static int ExtractReferenceNumber(string text)
    {
        var match = Regex.Match(text, @"(?:referencia|ref\.?|código|codigo|joya|reloj|artículo|articulo|#)\s*#?\s*(\d{2,6})\b", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var r))
        {
            return r;
        }
        return 0;
    }

    private static string? ExtractNumberSequence(string text, int minLength, int maxLength)
    {
        var match = Regex.Match(text, $@"\b\d{{{minLength},{maxLength}}}\b");
        return match.Success ? match.Value : null;
    }
}

// Modelos para deserializar respuesta de Gemini
internal class GeminiApiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

internal class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

internal class GeminiPart
{
    public string? Text { get; set; }
}
