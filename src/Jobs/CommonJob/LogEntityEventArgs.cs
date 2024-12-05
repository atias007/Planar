using Planar;
using System;

namespace CommonJob;

public class LogEntityEventArgs(LogEntity log, string fireInstanceId) : EventArgs
{
    public LogEntity Log { get; } = log;
    public string FireInstanceId { get; } = fireInstanceId;
}