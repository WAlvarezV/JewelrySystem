using JewerlySystem.Common.Application.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Common.Application;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Hardening ISO 27701 / OWASP A09: No serializar payloads sensibles en texto plano
        if (DataSanitizer.IsSensitive(requestName))
        {
            _logger.LogInformation("Processing {RequestName}: [REDACTED_SECURITY_DATA]", requestName);
        }
        else
        {
            _logger.LogInformation("Processing {RequestName}: {@Request}", requestName, request);
        }

        var response = await next();

        _logger.LogInformation("Completed {RequestName}", requestName);

        return response;
    }
}
