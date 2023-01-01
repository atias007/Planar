using Microsoft.AspNetCore.Mvc;

namespace Planar.Attributes
{
    public class ServiceUnavailableProducesAttribute : ProducesResponseTypeAttribute
    {
        public ServiceUnavailableProducesAttribute() : base(503)
        {
        }
    }
}