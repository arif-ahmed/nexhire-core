using MediatR;
using Microsoft.Extensions.Logging;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Shared.Infrastructure.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Starting request {RequestName}", requestName);

        var result = await next();

        if (result is Result { IsFailure: true } r)
        {
            _logger.LogWarning(
                "Request {RequestName} failed with error {ErrorCode}: {ErrorMessage}",
                requestName,
                r.Error.Code,
                r.Error.Message);
        }
        else
        {
            _logger.LogInformation("Request {RequestName} completed successfully", requestName);
        }

        return result;
    }
}
