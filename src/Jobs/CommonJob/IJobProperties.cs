using System.Collections.Generic;

namespace CommonJob;

public interface IJobProperties
{
    void FillGlobalConfigPlaceholder(Dictionary<string, string?> parameters);
}