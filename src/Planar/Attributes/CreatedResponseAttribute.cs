using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Planar.Attributes
{
    public class CreatedResponseAttribute : SwaggerResponseAttribute
    {
        public CreatedResponseAttribute(Type type) : base(201)
        {
            Type = type;
            ContentTypes = new[] { "application/json" };
        }

        public CreatedResponseAttribute() : base(201)
        {
            Type = null;
        }
    }
}