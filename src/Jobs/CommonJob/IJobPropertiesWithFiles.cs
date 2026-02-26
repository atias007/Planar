using System.Collections.Generic;

namespace CommonJob;

public interface IJobPropertiesWithFiles
{
    IEnumerable<string> Files { get; }
}