using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Geexbox.FrontendClient.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geexbox.FrontendClient
{
    public static class AngularCliMiddleware
    {
        private const string LogCategoryName = "Microsoft.AspNetCore.SpaServices";
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5.0);

        public static void Attach(ISpaBuilder spaBuilder)
        {
            string sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentException("Cannot be null or empty", "sourcePath");
            ILogger logger =
                spaBuilder.ApplicationBuilder.ApplicationServices.GetService<ILogger<AngularCliServerInfo>>();
            Task<Uri> targetUriTask = AngularCliMiddleware.StartAngularCliServerAsync(sourcePath, logger).ContinueWith<Uri>((Func<Task<AngularCliMiddleware.AngularCliServerInfo>, Uri>)(task => new UriBuilder("http", "localhost", task.Result.Port).Uri));
            spaBuilder.UseProxyToSpaDevelopmentServer((Func<Task<Uri>>)(() =>
            {
                TimeSpan startupTimeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout<Uri>(startupTimeout, "The Angular CLI process did not start listening for requests " + string.Format("within the timeout period of {0} seconds. ", (object)startupTimeout.Seconds) + "Check the log output for error information.");
            }));
        }

        private static async Task<AngularCliMiddleware.AngularCliServerInfo> StartAngularCliServerAsync(
          string sourcePath,
          ILogger logger)
        {
            logger.LogInformation("Starting @angular/cli ...");
            NpmScriptRunner npmScriptRunner = new NpmScriptRunner(sourcePath, "start", (IDictionary<string, string>)null);
            npmScriptRunner.AttachToLogger(logger);
            Match match;
            using (EventedStreamStringReader stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    match = await npmScriptRunner.StdOut.WaitForMatch(new Regex("open your browser on (http\\S+)", RegexOptions.None, AngularCliMiddleware.RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException("The NPM script 'start' exited without indicating that the Angular CLI was listening for requests. The error output was: " + stdErrReader.ReadAsString(), (Exception)ex);
                }
            }
            Uri cliServerUri = new Uri(match.Groups[1].Value);
            AngularCliMiddleware.AngularCliServerInfo serverInfo = new AngularCliMiddleware.AngularCliServerInfo()
            {
                Port = cliServerUri.Port
            };
            await AngularCliMiddleware.WaitForAngularCliServerToAcceptRequests(cliServerUri);
            return serverInfo;
        }

        private static async Task WaitForAngularCliServerToAcceptRequests(Uri cliServerUri)
        {
            int timeoutMilliseconds = 1000;
            using (HttpClient client = new HttpClient())
            {
                while (true)
                {
                    do
                    {
                        int num;
                        do
                        {
                            try
                            {
                                HttpResponseMessage httpResponseMessage = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, cliServerUri), new CancellationTokenSource(timeoutMilliseconds).Token);
                                goto label_12;
                            }
                            catch (Exception ex)
                            {
                                num = 1;
                            }
                        }
                        while (num != 1);
                        await Task.Delay(500);
                    }
                    while (timeoutMilliseconds >= 10000);
                    timeoutMilliseconds += 3000;
                }
            }
        label_12:;
        }

        private class AngularCliServerInfo
        {
            public int Port { get; set; }
        }
    }
}
