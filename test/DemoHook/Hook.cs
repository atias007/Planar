using Planar.Hook;
using System;
using System.Collections.Generic;
using System.Text;

namespace DemoHook;

internal class Hook : BaseHook
{
    public override string Name => "Planar.DemoHook";

    public override string Description =>
        """
        This is test demo hook for Planar. It is not implemented yet.
        Try to implement more than one line description.
        Even you can use multiple lines description if you want.
        """;

    public override Task Handle(IMonitorDetails monitorDetails)
    {
        LogInformation("Handle monitor details");
        return Task.CompletedTask;
    }

    public override Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        LogInformation("Handle System monitor details");
        return Task.CompletedTask;
    }
}