using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Geexbox.FrontendClient
{
    public class NpmScriptRunner
    {
        private static Regex AnsiColorRegex = new Regex("\x001B\\[[0-9;]*m", RegexOptions.None, TimeSpan.FromSeconds(1.0));

        public EventedStreamReader StdOut { get; }

        public EventedStreamReader StdErr { get; }

        public NpmScriptRunner(
          string workingDirectory,
          string scriptName,
          IDictionary<string, string> envVars)
        {
            if (string.IsNullOrEmpty(workingDirectory))
                throw new ArgumentException("Cannot be null or empty.", nameof(workingDirectory));
            if (string.IsNullOrEmpty(scriptName))
                throw new ArgumentException("Cannot be null or empty.", nameof(scriptName));
            string fileName = "npm";
            string str = "run " + scriptName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd";
                str = "/c npm " + str;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
            {
                Arguments = str,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            if (envVars != null)
            {
                foreach (KeyValuePair<string, string> envVar in (IEnumerable<KeyValuePair<string, string>>)envVars)
                    startInfo.Environment[envVar.Key] = envVar.Value;
            }
            Process process = NpmScriptRunner.LaunchNodeProcess(startInfo);
            this.StdOut = new EventedStreamReader(process.StandardOutput);
            this.StdErr = new EventedStreamReader(process.StandardError);
        }

        public void AttachToLogger(ILogger logger)
        {
            this.StdOut.OnReceivedLine += (EventedStreamReader.OnReceivedLineHandler)(line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                    return;
                logger.LogInformation(NpmScriptRunner.StripAnsiColors(line));
            });
            this.StdErr.OnReceivedLine += (EventedStreamReader.OnReceivedLineHandler)(line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                    return;
                logger.LogError(NpmScriptRunner.StripAnsiColors(line));
            });
            this.StdErr.OnReceivedChunk += (EventedStreamReader.OnReceivedChunkHandler)(chunk =>
            {
                if (Array.IndexOf<char>(chunk.Array, '\n', chunk.Offset, chunk.Count) >= 0)
                    return;
                Console.Write(chunk.Array, chunk.Offset, chunk.Count);
            });
        }

        private static string StripAnsiColors(string line) => NpmScriptRunner.AnsiColorRegex.Replace(line, string.Empty);

        private static Process LaunchNodeProcess(ProcessStartInfo startInfo)
        {
            try
            {
                Process process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;
                return process;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start 'npm'. To resolve this:.\n\n[1] Ensure that 'npm' is installed and can be found in one of the PATH directories.\n    Current PATH enviroment variable is: " + Environment.GetEnvironmentVariable("PATH") + "\n    Make sure the executable is in one of those directories, or update your PATH.\n\n[2] See the InnerException for further details of the cause.", ex);
            }
        }
    }
}
