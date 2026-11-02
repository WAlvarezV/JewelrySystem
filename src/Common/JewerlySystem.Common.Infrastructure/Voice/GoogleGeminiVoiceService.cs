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

            var model = _configuration["GoogleGemini:Model"] ?? "gemini-3.5-flash-lite";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Error al invocar Google Gemini API: {StatusCode} - {Error}", response.StatusCode, errorBody);
                return ParseHeuristically(transcription, $"Error en API Gemini ({response.StatusCode}). Fallback local ejecutado.");
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
    /// Parser heurístico local para cuando no hay conexión o no se ha configurado la API Key de Gemini
    /// </summary>
    private static VoiceOperationResult ParseHeuristically(string text, string? notice = null)
    {
        var lower = text.ToLowerInvariant();
        var result = new VoiceOperationResult
        {
            RawTranscription = text,
            Confidence = 0.75
        };

        // 1. Venta de artículo en inventario (requiere referencia obligatoria)
        if (lower.Contains("venta") || lower.Contains("vender") || lower.Contains("descargar") || lower.Contains("descargue"))
        {
            result.Intent = VoiceIntents.CrearVenta;
            var (val, _) = ExtractMonetaryValues(text);
            var refNum = ExtractReferenceNumber(text);
            var custName = ExtractCustomerName(text);
            var custId = ExtractNumberSequence(text, 6, 12);

            result.Sale = new VoiceSalePayload
            {
                Reference = refNum,
                SaleValue = val > 0 ? val : 250000,
                Description = text,
                CustomerName = custName,
                CustomerIdentification = custId,
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : (lower.Contains("tarjeta") ? "TARJETA" : "EFECTIVO")
            };

            if (refNum <= 0)
            {
                result.Summary = $"Venta detectada por ${(result.Sale.SaleValue):N0}, pero falta el número de referencia para descargar el inventario.";
                result.MissingFields.Add("Número de referencia del artículo (requerido para descargarlo de inventario)");
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
            var (value, initial) = ExtractMonetaryValues(text);
            var customerName = ExtractCustomerName(text);

            result.Contract = new VoiceContractPayload
            {
                CustomerName = customerName,
                Description = text,
                Value = value > 0 ? value : 500000,
                InitialPayment = initial > 0 ? initial : (value > 0 ? value / 2 : 100000),
                Weight = ExtractWeight(text),
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : "EFECTIVO"
            };

            result.Summary = $"Contrato para {result.Contract.CustomerName ?? "Cliente"} por ${(result.Contract.Value):N0} con abono de ${(result.Contract.InitialPayment):N0}.";
        }
        // 3. Cliente
        else if (lower.Contains("cliente") || lower.Contains("persona") || lower.Contains("registrar cliente") || lower.Contains("nuevo cliente"))
        {
            result.Intent = VoiceIntents.CrearCliente;
            result.Customer = new VoiceCustomerPayload
            {
                FullName = ExtractCustomerName(text) ?? "Cliente Dictado",
                IdentificationNumber = ExtractNumberSequence(text, 6, 12) ?? "10" + Random.Shared.Next(10000000, 99999999),
                CellPhone = ExtractNumberSequence(text, 10, 10) ?? "300" + Random.Shared.Next(1000000, 9999999)
            };
            result.Summary = $"Registrar cliente {result.Customer.FullName} con documento {result.Customer.IdentificationNumber}.";
        }
        // 4. Movimiento de Caja (Ingreso o Egreso general)
        else if (lower.Contains("caja") || lower.Contains("ingreso") || lower.Contains("gasto") || lower.Contains("egreso"))
        {
            result.Intent = VoiceIntents.CrearMovimientoCaja;
            var (val, _) = ExtractMonetaryValues(text);
            var isEgreso = lower.Contains("gasto") || lower.Contains("egreso") || lower.Contains("pago de servicio");

            result.Cash = new VoiceCashPayload
            {
                Description = text,
                Value = val > 0 ? val : 150000,
                RecordType = isEgreso ? "EGRESO" : "INGRESO",
                PaymentMethod = lower.Contains("transferencia") ? "TRANSFERENCIA" : (lower.Contains("tarjeta") ? "TARJETA" : "EFECTIVO")
            };
            result.Summary = $"{(isEgreso ? "Egreso" : "Ingreso")} de caja por ${(result.Cash.Value):N0} en {result.Cash.PaymentMethod}.";
        }
        else
        {
            result.Intent = VoiceIntents.Desconocido;
            result.Summary = "No se detectó una operación comercial clara (contrato, cliente, venta o movimiento de caja).";
            result.MissingFields.Add("Operación no identificada");
        }

        if (!string.IsNullOrWhiteSpace(notice))
        {
            result.Summary = $"[Modo Local] {result.Summary}";
        }

        return result;
    }

    private static (int Total, int Initial) ExtractMonetaryValues(string text)
    {
        // Buscar patrones como "2 millones", "500 mil", "$1.500.000", "2000000"
        var total = 0;
        var initial = 0;

        var millonMatch = Regex.Match(text, @"(\d+)\s*(?:millón|millon|millones)", RegexOptions.IgnoreCase);
        if (millonMatch.Success && int.TryParse(millonMatch.Groups[1].Value, out var m))
        {
            total = m * 1_000_000;
        }

        var milMatch = Regex.Match(text, @"(?:abono\s*(?:de)?|anticipo\s*(?:de)?)\s*(\d+)\s*(?:mil)", RegexOptions.IgnoreCase);
        if (milMatch.Success && int.TryParse(milMatch.Groups[1].Value, out var k))
        {
            initial = k * 1_000;
        }
        else
        {
            var anyMil = Regex.Matches(text, @"(\d+)\s*(?:mil)", RegexOptions.IgnoreCase);
            if (anyMil.Count > 0 && int.TryParse(anyMil[0].Groups[1].Value, out var k2))
            {
                if (total == 0) total = k2 * 1_000;
                else initial = k2 * 1_000;
            }
        }

        // Buscar números crudos grandes
        var numberMatches = Regex.Matches(text, @"(?:\$?\s*)(\d{1,3}(?:\.\d{3})+|\d{4,9})");
        foreach (Match match in numberMatches)
        {
            var raw = match.Groups[1].Value.Replace(".", "");
            if (int.TryParse(raw, out var n))
            {
                if (total == 0) total = n;
                else if (initial == 0) initial = n;
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
        var match = Regex.Match(text, @"(?:cliente|para|señor|señora|don|doña)\s+([A-ZÁÉÍÓÚÑa-záéíóúñ]+(?:\s+[A-ZÁÉÍÓÚÑa-záéíóúñ]+){1,3})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
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
