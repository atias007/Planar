using Microsoft.AspNetCore.Mvc;

namespace Planar.Attributes
{
    public class AcceptedContentResponseAttribute : ProducesResponseTypeAttribute
    {
        public AcceptedContentResponseAttribute() : base(202)
        {
        }
    }
}