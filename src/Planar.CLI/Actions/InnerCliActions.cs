using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    [Module("inner", "inner actions")]
    public class InnerCliActions : BaseCliAction<InnerCliActions>
    {
        [Action("cls")]
        [Action("clear")]
        public static async Task<CliActionResponse> Clear(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException("task was canceled"); }
            Console.Clear();
            return await Task.FromResult(CliActionResponse.Empty);
        }

        [Action("whoami")]
        public static async Task<CliActionResponse> WhoAmI(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException("task was canceled"); }
            var title =
                string.IsNullOrEmpty(LoginProxy.Username) ?
                CliConsts.Anonymous :
                $"{LoginProxy.Username} ({LoginProxy.Role?.ToLower()})";

            var result = new CliActionResponse(null, message: title);
            return await Task.FromResult(result);
        }

        [Action("help")]
        public static async Task<CliActionResponse> Help(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CliHelpGenerator.ShowModules();
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