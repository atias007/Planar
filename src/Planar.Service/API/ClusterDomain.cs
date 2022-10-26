using Planar.Service.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ClusterDomain : BaseBL<ClusterDomain>
    {
        public ClusterDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<ClusterNode>> GetNodes()
        {
            return await DataLayer.GetClusterNodes();
        }
    }
}