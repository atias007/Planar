using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
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

        [Action("sleep")]
        public static async Task<CliActionResponse> Sleep(CliSleepRequest request, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException("task was canceled"); }
            if (request.Seconds <= 0) { throw new ArgumentException("seconds must be greater than 0"); }
            if (request.Seconds > 1_200) { throw new ArgumentException("seconds must be less than or equals 1200 seconds"); }

            await Console.Out.WriteLineAsync($"sleeping {request.Seconds:N0} seconds...   ");

            for (var i = 0; i < request.Seconds; i++)
            {
                if (cancellationToken.IsCancellationRequested) { throw new TaskCanceledException("task was canceled"); }
                await Task.Delay(1000, cancellationToken);
                Console.CursorLeft = 0;
                Console.CursorTop -= 1;
                await Console.Out.WriteLineAsync($"sleeping {request.Seconds - 1 - i:N0} seconds...   ");
            }

            Console.CursorLeft = 0;
            Console.CursorTop -= 1;
            Console.WriteLine(string.Empty.PadLeft(25, ' '));
            Console.CursorLeft = 0;
            Console.CursorTop -= 1;
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