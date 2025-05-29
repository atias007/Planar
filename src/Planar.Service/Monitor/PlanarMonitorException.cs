using System;

namespace Planar.Service.Monitor;

public class PlanarMonitorException(string message) : Exception(message)
{
}