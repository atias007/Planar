using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.SystemJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    internal class SummaryReport
    {
        private readonly ILogger<SummaryReport> _logger;
        private readonly IServiceScopeFactory _serviceScope;

        public SummaryReport(IServiceScopeFactory serviceScope, ILogger<SummaryReport> logger)
        {
            _logger = logger;
            _serviceScope = serviceScope;
        }
    }
}