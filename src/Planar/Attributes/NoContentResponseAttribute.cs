using Microsoft.AspNetCore.Mvc;

namespace Planar.Attributes
{
    public class NoContentResponseAttribute : ProducesResponseTypeAttribute
    {
        public NoContentResponseAttribute() : base(204)
        {
        }
    }
}