namespace Planar.API.Common.Entities
{
    public class SetJobAuthorRequest : JobOrTriggerKey
    {
        public string Author { get; set; } = null!;
    }
}