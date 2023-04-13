using Planar.API.Common.Entities;

namespace Planar.Service.Model
{
    internal class AddJobDynamicRequest : SetJobRequest
    {
        public virtual dynamic Properties { get; set; } = null!;
    }
}