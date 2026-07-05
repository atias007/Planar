using System.Collections.Generic;

namespace CommonJob;

public interface IJobProperties
{
    void SetGlobalConfigPlaceholder(Dictionary<string, string?> parameters);
}
