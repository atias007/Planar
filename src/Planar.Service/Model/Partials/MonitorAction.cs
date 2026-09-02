using Planar.API.Common.Entities;
using System;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace Planar.Service.Model;

public partial class MonitorAction
{
    private readonly Lazy<MonitorEventArguments?> _eventArguments;

    public MonitorAction()
    {
        _eventArguments = new Lazy<MonitorEventArguments?>(GetEventArgumentsInner, isThreadSafe: true);
    }

    public MonitorEventArguments? GetEventArguments() => _eventArguments.Value;

    private MonitorEventArguments? GetEventArgumentsInner()
    {
        if (string.IsNullOrEmpty(EventArgument)) { return null; }

        try
        {
            var arguments = new Deserializer().Deserialize<MonitorEventArguments>(EventArgument);
            return arguments;
        }
        catch (Exception)
        {
            return null;
        }
    }
}