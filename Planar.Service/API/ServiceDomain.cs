using Microsoft.Extensions.Logging;
using Planar.Service.Data;

namespace Planar.Service.API
{
    public class ServiceDomain : BaseBL<ServiceDomain>
    {
        public ServiceDomain(DataLayer dataLayer, ILogger<ServiceDomain> logger) : base(dataLayer, logger)
        {
        }
    }
}