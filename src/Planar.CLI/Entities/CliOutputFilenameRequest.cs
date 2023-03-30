using Planar.CLI.Attributes;
using Planar.CLI.Exceptions;
using System.IO;
using System.Linq;

namespace Planar.CLI.Entities
{
    public abstract class CliOutputFilenameRequest
    {
        [ActionProperty("o", "output")]
        public string? OutputFilename { get; set; }

        public void Validate()
        {
            if (!HasOutputFilename) { return; }

            if (Path.GetInvalidFileNameChars().Any(c => OutputFilename != null && OutputFilename.Contains(c)))
            {
                throw new CliValidationException($"output filename '{OutputFilename}' is invalid");
            }
        }

        public bool HasOutputFilename => !string.IsNullOrEmpty(OutputFilename);
    }
}