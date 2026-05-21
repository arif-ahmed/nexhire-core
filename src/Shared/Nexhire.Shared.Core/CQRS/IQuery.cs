using MediatR;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Shared.Core.CQRS;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
