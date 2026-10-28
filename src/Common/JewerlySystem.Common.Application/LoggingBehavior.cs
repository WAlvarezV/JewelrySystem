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

        _logger.LogInformation("Processing {RequestName}: {@Request}", requestName, request);

        var response = await next();

        _logger.LogInformation("Completed {RequestName}", requestName);

        return response;
    }
}
