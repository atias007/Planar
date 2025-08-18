using Planar.API.Common.Entities;
using System.Collections.Generic;

namespace Planar.CLI.Entities;

public class JobHistoryOdataWrapper
{
    public IEnumerable<JobHistory> Value { get; set; } = [];
}