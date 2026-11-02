using System.Net.Http.Headers;
using System.Net.Http.Json;
using JewerlySystem.Common.Application.Emailing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Common.Infrastructure.Emailing;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var fromEmail = _configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_RESEND_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Resend:ApiKey no configurada. Simulación de envío a {To}: {Subject}", toEmail, subject);
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            var payload = new
            {
                from = $"JewerlySystem <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlContent
            };

            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Correo OTP enviado exitosamente a {To} mediante Resend.", toEmail);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Resend respondió con código {Code} al enviar a {To}: {Error}", response.StatusCode, toEmail, errorContent);

            // Respaldo Sandbox de Resend: si está en modo prueba y solo permite enviar al correo del propietario (ingnova.team@gmail.com)
            if (errorContent.Contains("can only send testing emails to your own email address (ingnova.team@gmail.com)")
                && !toEmail.Equals("ingnova.team@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Reenviando copia de sandbox a ingnova.team@gmail.com para {To}...", toEmail);
                using var fallbackRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                fallbackRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                var fallbackPayload = new
                {
                    from = $"JewerlySystem <{fromEmail}>",
                    to = new[] { "ingnova.team@gmail.com" },
                    subject = $"[Copia Sandbox para {toEmail}] {subject}",
                    html = $"<div style='background-color:#fef3c7;padding:12px;border-radius:8px;margin-bottom:15px;color:#92400e;font-size:13px;border:1px solid #fde68a;'><strong>Aviso de Sandbox Resend:</strong> Correo dirigido originalmente a <code>{toEmail}</code>.</div>" + htmlContent
                };
                fallbackRequest.Content = JsonContent.Create(fallbackPayload);
                var fallbackResponse = await _httpClient.SendAsync(fallbackRequest);
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Copia de sandbox entregada con éxito a ingnova.team@gmail.com");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada enviando correo Resend a {To}", toEmail);
            return false;
        }
    }
}
