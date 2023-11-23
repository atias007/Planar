namespace Planar.Hooks
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Custom Exception")]
    public sealed class PlanarHookException : Exception
    {
        public PlanarHookException(string message) : base(message)
        {
        }

        public PlanarHookException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}