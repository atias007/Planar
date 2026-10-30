using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Planar.Job")]
[assembly: InternalsVisibleTo("Planar.Hook")]
[assembly: InternalsVisibleTo("Planar.Test")]
[assembly: InternalsVisibleTo("Planar.Job.Test")]
[assembly: InternalsVisibleTo("Planar.Job.RabbitMQ")]
[assembly: InternalsVisibleTo("Planar.Job.Http")]

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