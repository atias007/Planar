using Microsoft.AspNetCore.Mvc;

namespace Planar.Attributes;

public class ForbiddenResponseAttribute : ProducesResponseTypeAttribute
{
    public ForbiddenResponseAttribute() : base(403)
    {
    }
}