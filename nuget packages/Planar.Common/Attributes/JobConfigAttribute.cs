using System;

namespace Planar.Job
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class JobConfigAttribute : DataAttribute
    {
        public JobConfigAttribute(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be null or whitespace.", nameof(filename));
            }

            if (!filename.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Filename must have a .yml extension.", nameof(filename));
            }

            Filename = filename;
        }

        public string Filename { get; private set; }

        internal string GetEnvironmentFilename(string environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                return string.Empty;
            }

            var extensionIndex = Filename.LastIndexOf(".yml", StringComparison.OrdinalIgnoreCase);
            if (extensionIndex < 0)
            {
                return string.Empty;
            }

            return $"{Filename.Substring(0, extensionIndex)}.{environment}.yml";
        }
    }
}