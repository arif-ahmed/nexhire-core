using MediatR;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Shared.Core.CQRS;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
