namespace Planar.Service.General.Hash
{
    internal class HashEntity
    {
        public string Value { get; init; } = null!;
        public byte[] Hash { get; init; } = null!;
        public byte[] Salt { get; init; } = null!;
    }
}