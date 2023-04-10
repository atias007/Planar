using Planar.API.Common.Entities;

namespace Planar.Service.Model
{
    internal class SetJobDynamicRequest : SetJobRequest
    {
        public virtual dynamic? Properties { get; set; }
    }
}