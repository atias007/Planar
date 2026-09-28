using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Common;

public interface IJobResourceDataLayer
{
    Task<string?> GetResourceValue(string name);

    Task<Dictionary<string, string>> GetResources(IEnumerable<string> names, CancellationToken cancellationToken);
}