namespace Planar.Hooks
{
    [Serializable]
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