using Quartz;
using System;

namespace Planar.Service.Exceptions;

public sealed class JobNotFoundException(JobKey key) : Exception
{
    public JobKey Key => key;
}