namespace Planar.API.Common.Entities;

public class PlanarIdResponseWrapper
{
    public PlanarIdResponseWrapper(string id, bool unchanged)
    {
        PlanarId = new PlanarIdResponse { Id = id };
        Unchanged = unchanged;
    }

    public PlanarIdResponse PlanarId { get; set; }
    public bool Unchanged { get; private set; }
}