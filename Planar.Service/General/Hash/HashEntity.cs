namespace Planar.Service.General.Hash
{
    internal class HashEntity
    {
        public string Value { get; set; }
        public byte[] Hash { get; set; }
        public byte[] Salt { get; set; }
    }
}