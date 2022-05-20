using Microsoft.Extensions.Logging;
using System;

namespace Planar.Service.API
{
    public class ServiceDomain : BaseBL<ServiceDomain>
    {
        public ServiceDomain(ILogger<ServiceDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }
    }
}