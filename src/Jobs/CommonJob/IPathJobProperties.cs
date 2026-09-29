using System.Collections;
using System.Collections.Generic;

namespace CommonJob;

// TODO: to be removed
public interface IPathJobProperties
{
    public string Path { get; }
}

public interface IJobPropertiesWithFiles
{
    IEnumerable<string> Files { get; }
}