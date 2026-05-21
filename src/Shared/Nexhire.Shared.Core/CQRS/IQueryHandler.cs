using MediatR;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Shared.Core.CQRS;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
