using Microsoft.Extensions.Logging;
using Planar.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Planar.Service.Monitor
{
    internal sealed class HookExecuter : IDisposable
    {
        private readonly List<string> _output = new();
        private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(3);
        private readonly ILogger _logger;
        private readonly string _filename;
        private Process? _process;

        public HookExecuter(ILogger logger, string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new PlanarMonitorException($"filename hook argument is null or empty");
            }

            _logger = logger;
            _filename = filename;
        }

        public Task HandleByExternalHook(MonitorDetails details)
        {
            try
            {
                ValidateFileExists(_filename);
                var wrapper = new MonitorMessageWrapper(details);
                var json = JsonSerializer.Serialize(wrapper);
                var startInfo = GetProcessStartInfo(_filename!, json);
                var success = StartProcess(startInfo, _timeout);
                if (!success)
                {
                    OnTimeout();
                }
            }
            finally
            {
                FinalizeProcess();
                AnalyzeOutput();
            }

            return Task.CompletedTask;
        }

        public Task HandleSystemByExternalHook(MonitorSystemDetails details)
        {
            try
            {
                ValidateFileExists(_filename);
                var wrapper = new MonitorMessageWrapper(details);
                var json = JsonSerializer.Serialize(wrapper);
                var startInfo = GetProcessStartInfo(_filename, json);
                var success = StartProcess(startInfo, _timeout);
                if (!success)
                {
                    OnTimeout();
                }
            }
            finally
            {
                FinalizeProcess();
                AnalyzeOutput();
            }

            return Task.CompletedTask;
        }

        private static void ValidateFileExists(string? path)
        {
            if (path == null)
            {
                throw new PlanarMonitorException($"monitor hook filename is null or empty");
            }

            if (!File.Exists(path))
            {
                throw new PlanarMonitorException($"monitor hook filename '{path}' could not be found");
            }
        }

        private static ProcessStartInfo GetProcessStartInfo(string filename, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64String = Convert.ToBase64String(bytes);
            var startInfo = GetProcessStartInfo(filename);
            startInfo.Arguments = $"--planar-service-mode --context {base64String}";
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            SetProcessToLinuxOs(startInfo);
            return startInfo;
        }

        private static ProcessStartInfo GetProcessStartInfo(string filename)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = filename,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = new FileInfo(filename).Directory?.FullName,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            return startInfo;
        }

        private static void SetProcessToLinuxOs(ProcessStartInfo startInfo)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var filename = GetFilenameFoLinux(startInfo.FileName);
                startInfo.FileName = "dotnet.exe";
                startInfo.Arguments = $"\"{filename}\" {startInfo.Arguments}";
            }
        }

        private static string GetFilenameFoLinux(string filename)
        {
            var fi = new FileInfo(filename);
            if (string.Equals(fi.Extension, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                var newFileName = $"{fileNameWithoutExtension}.dll";
                return newFileName;
            }
            else
            {
                return filename;
            }
        }

        private bool StartProcess(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                throw new PlanarException($"could not start process {_filename}");
            }

            _process.EnableRaisingEvents = true;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _process.OutputDataReceived += ProcessOutputDataReceived;
            _process.ErrorDataReceived += ProcessOutputDataReceived;

            _process.WaitForExit(Convert.ToInt32(timeout.TotalMilliseconds));
            if (!_process.HasExited)
            {
                _logger.LogError("Process timeout expire. Timeout was {Timeout}", $"{timeout: hh\\:mm\\:ss}");
                return false;
            }

            return true;
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data)) { return; }
            _output.Add(eventArgs.Data);
        }

        private void OnTimeout()
        {
            Kill("timeout expire");
        }

        private void Kill(string reason)
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                _logger.LogWarning("Process was stopped. Reason: {Reason}", reason);
                _process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to kill process job {Filename}", _process.StartInfo.FileName);
            }
        }

        private void FinalizeProcess()
        {
            try { _process?.CancelErrorRead(); } catch { DoNothingMethod(); }
            try { _process?.CancelOutputRead(); } catch { DoNothingMethod(); }
            try { _process?.Close(); } catch { DoNothingMethod(); }
            try { _process?.Dispose(); } catch { DoNothingMethod(); }
            try { if (_process != null) { _process.EnableRaisingEvents = false; } } catch { DoNothingMethod(); }
            UnsubscribeOutput();
        }

        private void AnalyzeOutput()
        {
            const string pattern = "^<hook\\.log\\.(trace|debug|information|warning|error|critical)>.+<\\/hook\\.log\\.(trace|debug|information|warning|error|critical)>$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            foreach (var item in _output)
            {
                var matches = regex.Matches(item);
                if (
                    matches.Any() &&
                    matches[0].Success &&
                    matches[0].Groups.Count == 3 &&
                    matches[0].Groups[1].Value == matches[0].Groups[2].Value)
                {
                    var doc = XDocument.Parse(matches[0].Groups[0].Value);
                    var message = doc.Root?.Value;
                    var level = matches[0].Groups[1].Value;
                    if (!Enum.TryParse<LogLevel>(level, ignoreCase: true, out var logLevel)) { continue; }
                    _logger.Log(logLevel, message);
                }
            }
        }

        private static void DoNothingMethod()
        {
            //// *** Do Nothing Method *** ////
        }

        private void UnsubscribeOutput()
        {
            if (_process == null) { return; }
            try { _process.ErrorDataReceived -= ProcessOutputDataReceived; } catch { DoNothingMethod(); }
            try { _process.OutputDataReceived -= ProcessOutputDataReceived; } catch { DoNothingMethod(); }
        }

        public void Dispose()
        {
            FinalizeProcess();
            _process = null;
        }
    }
}