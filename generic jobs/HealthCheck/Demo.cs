using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheck;

internal partial class Job
{
    static partial void VetoHost(Host host)
    {
        if (host.Name == "http://127.0.0.1")
        {
            host.Veto = true;
        }
    }
}