using System.Collections.Generic;

namespace CommonJob;

public interface IResourceJobProperties
{
    public IEnumerable<string> ResourceNames { get; }
}