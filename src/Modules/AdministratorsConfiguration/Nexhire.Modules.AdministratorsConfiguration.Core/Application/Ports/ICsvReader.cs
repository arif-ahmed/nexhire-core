using Nexhire.Modules.AdministratorsConfiguration.Core.Domain.Services;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.AdministratorsConfiguration.Core.Application.Ports;

public interface ICsvReader
{
    Result<IReadOnlyList<RawImportRow>> Read(Stream csvContent);
}
