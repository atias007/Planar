using Microsoft.AspNetCore.Mvc;

namespace Planar.Attributes
{
    public class JsonConsumesAttribute : ConsumesAttribute
    {
        public JsonConsumesAttribute() : base("application/json")
        {
        }
    }
}