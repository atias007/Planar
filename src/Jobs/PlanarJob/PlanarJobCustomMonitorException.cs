using System;

namespace PlanarJob;

public sealed class PlanarJobCustomMonitorException(string message) : Exception(message)
{
}