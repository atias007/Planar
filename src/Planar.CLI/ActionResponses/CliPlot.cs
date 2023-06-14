using System.Collections.Generic;

namespace Planar.CLI.CliGeneral
{
    public class CliPlot
    {
        public CliPlot(IEnumerable<double> series)
        {
            Series = series;
        }

        public IEnumerable<double> Series { get; private set; }
    }
}