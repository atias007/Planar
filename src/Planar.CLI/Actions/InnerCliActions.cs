using Planar.CLI.Attributes;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("inner", "inner actions")]
    public class InnerCliActions : BaseCliAction<InnerCliActions>
    {
        [Action("cls")]
        public static async Task<CliActionResponse> GetParameter(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException("task was canceled"); }
            Console.Clear();
            return await Task.FromResult(CliActionResponse.Empty);
        }

        ////[Action("plot")]
        ////public static async Task<CliActionResponse> Plot(CancellationToken cancellationToken = default)
        ////{
        ////    var series = new double[100];
        ////    for (var i = 0; i < series.Length; i++)
        ////    {
        ////        series[i] = 15 * Math.Sin(i * ((Math.PI * 4) / series.Length));
        ////    }

        ////    Console.OutputEncoding = Encoding.UTF8;
        ////    var options = new AsciiChart.Sharp.Options
        ////    {
        ////        AxisColor = AsciiChart.Sharp.AnsiColor.Blue,
        ////        Fill = '.',
        ////        LabelColor = AsciiChart.Sharp.AnsiColor.Red
        ////    };

        ////    Console.WriteLine(AsciiChart.Sharp.AsciiChart.Plot(series, options));
        ////    return await Task.FromResult(CliActionResponse.Empty);
        ////}
    }
}