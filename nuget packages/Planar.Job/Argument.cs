namespace Planar
{
    internal class Argument
    {
#if NETSTANDARD2_0
        public string Key { get; set; }
        public string Value { get; set; }
#else
        public string? Key { get; set; }
        public string? Value { get; set; }
#endif

        public override string ToString()
        {
            return $"{Key}: {Value}";
        }
    }
}