using Swashbuckle.AspNetCore.Annotations;

namespace Planar.Attributes
{
    public class ServiceUnavailableResponseAttribute : SwaggerResponseAttribute
    {
        public ServiceUnavailableResponseAttribute() : base(503)
        {
        }
    }
}