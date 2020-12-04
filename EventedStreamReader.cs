using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geexbox.FrontendClient
{
    public class EventedStreamReader
    {
        private readonly StreamReader _streamReader;
        private readonly StringBuilder _linesBuffer;

        public event EventedStreamReader.OnReceivedChunkHandler OnReceivedChunk;

        public event EventedStreamReader.OnReceivedLineHandler OnReceivedLine;

        public event EventedStreamReader.OnStreamClosedHandler OnStreamClosed;

        public EventedStreamReader(StreamReader streamReader)
        {
            this._streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
            this._linesBuffer = new StringBuilder();
            Task.Factory.StartNew<Task>(new Func<Task>(this.Run));
        }

        public Task<Match> WaitForMatch(Regex regex)
        {
            TaskCompletionSource<Match> tcs = new TaskCompletionSource<Match>();
            object completionLock = new object();
            EventedStreamReader.OnReceivedLineHandler onReceivedLineHandler = (EventedStreamReader.OnReceivedLineHandler)null;
            EventedStreamReader.OnStreamClosedHandler onStreamClosedHandler = (EventedStreamReader.OnStreamClosedHandler)null;
            onReceivedLineHandler = (EventedStreamReader.OnReceivedLineHandler)(line =>
            {
                Match match = regex.Match(line);
                if (!match.Success)
                    return;
                ResolveIfStillPending((Action)(() => tcs.SetResult(match)));
            });
            onStreamClosedHandler = (EventedStreamReader.OnStreamClosedHandler)(() => ResolveIfStillPending((Action)(() => tcs.SetException((Exception)new EndOfStreamException()))));
            this.OnReceivedLine += onReceivedLineHandler;
            this.OnStreamClosed += onStreamClosedHandler;
            return tcs.Task;

            void ResolveIfStillPending(Action applyResolution)
            {
                lock (completionLock)
                {
                    if (tcs.Task.IsCompleted)
                        return;
                    this.OnReceivedLine -= onReceivedLineHandler;
                    this.OnStreamClosed -= onStreamClosedHandler;
                    applyResolution();
                }
            }
        }

        private async Task Run()
        {
            char[] buf = new char[8192];
            while (true)
            {
                int num1 = await this._streamReader.ReadAsync(buf, 0, buf.Length);
                if (num1 != 0)
                {
                    this.OnChunk(new ArraySegment<char>(buf, 0, num1));
                    int num2 = Array.IndexOf<char>(buf, '\n', 0, num1);
                    if (num2 < 0)
                    {
                        this._linesBuffer.Append(buf, 0, num1);
                    }
                    else
                    {
                        this._linesBuffer.Append(buf, 0, num2 + 1);
                        this.OnCompleteLine(this._linesBuffer.ToString());
                        this._linesBuffer.Clear();
                        this._linesBuffer.Append(buf, num2 + 1, num1 - (num2 + 1));
                    }
                }
                else
                    break;
            }
            this.OnClosed();
        }

        private void OnChunk(ArraySegment<char> chunk)
        {
            EventedStreamReader.OnReceivedChunkHandler onReceivedChunk = this.OnReceivedChunk;
            if (onReceivedChunk == null)
                return;
            onReceivedChunk(chunk);
        }

        private void OnCompleteLine(string line)
        {
            EventedStreamReader.OnReceivedLineHandler onReceivedLine = this.OnReceivedLine;
            if (onReceivedLine == null)
                return;
            onReceivedLine(line);
        }

        private void OnClosed()
        {
            EventedStreamReader.OnStreamClosedHandler onStreamClosed = this.OnStreamClosed;
            if (onStreamClosed == null)
                return;
            onStreamClosed();
        }

        public delegate void OnReceivedChunkHandler(ArraySegment<char> chunk);

        public delegate void OnReceivedLineHandler(string line);

        public delegate void OnStreamClosedHandler();
    }
}
