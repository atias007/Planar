namespace Planar.Client.Entities
{
    public class HookDetails
    {
#if NETSTANDARD2_0
        public string HookType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
#else
        public string HookType { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
#endif
    }
}