using System.IO;

namespace Planar.Job
{
    internal static class General
    {
        public static string GenerateId() => Path.GetRandomFileName().Replace(".", string.Empty);
    }
}